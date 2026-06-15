"use client";

import { useMemo, useState } from "react";
import { addToCart, type CartOption } from "@/lib/cart";
import { formatMoney } from "@/lib/format";
import type { MenuItem, MenuVariant } from "@/lib/api";

type Props = {
  item: MenuItem;
};

export default function AddToCartButton({ item }: Props) {
  const [addedKey, setAddedKey] = useState<string | null>(null);
  const [open, setOpen] = useState(false);

  const hasOptions = item.optionGroups.length > 0;

  function add(variantId: string | null, variantName: string | null, price: number, options: CartOption[]) {
    addToCart({
      menuItemId: item.id,
      variantId,
      name: item.name,
      variantName,
      price,
      currency: item.currency,
      options,
    });
    const key = `${variantId ?? "base"}:${options.map((o) => o.optionId).sort().join(",")}`;
    setAddedKey(key);
    setTimeout(() => setAddedKey((k) => (k === key ? null : k)), 1200);
  }

  // ── Modifiers / add-ons (and variant, when present) are chosen in a modal ──
  if (hasOptions) {
    return (
      <>
        <button
          type="button"
          onClick={() => setOpen(true)}
          className="rounded-lg bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700"
        >
          Add
        </button>
        {open && (
          <OptionsModal
            item={item}
            onClose={() => setOpen(false)}
            onConfirm={(variantId, variantName, price, options) => {
              add(variantId, variantName, price, options);
              setOpen(false);
            }}
          />
        )}
      </>
    );
  }

  if (item.variants.length > 0) {
    return (
      <div className="flex flex-col items-end gap-1.5">
        {item.variants.map((v) => (
          <button
            key={v.id}
            type="button"
            onClick={() => add(v.id, v.name, v.price, [])}
            className="rounded-lg bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700"
          >
            {addedKey === `${v.id}:` ? "Added ✓" : `${v.name} · ${formatMoney(v.price, item.currency)}`}
          </button>
        ))}
      </div>
    );
  }

  return (
    <button
      type="button"
      onClick={() => add(null, null, item.price, [])}
      className="rounded-lg bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700"
    >
      {addedKey === "base:" ? "Added ✓" : "Add"}
    </button>
  );
}

type ModalProps = {
  item: MenuItem;
  onClose: () => void;
  onConfirm: (variantId: string | null, variantName: string | null, price: number, options: CartOption[]) => void;
};

function OptionsModal({ item, onClose, onConfirm }: ModalProps) {
  const [variant, setVariant] = useState<MenuVariant | null>(item.variants[0] ?? null);
  // selected[groupId] = set of option ids
  const [selected, setSelected] = useState<Record<string, string[]>>({});
  const [error, setError] = useState<string | null>(null);

  const basePrice = variant ? variant.price : item.price;

  const chosen: CartOption[] = useMemo(() => {
    const out: CartOption[] = [];
    for (const g of item.optionGroups) {
      const ids = selected[g.id] ?? [];
      for (const o of g.options) {
        if (ids.includes(o.id)) out.push({ optionId: o.id, name: o.name, priceDelta: o.priceDelta });
      }
    }
    return out;
  }, [item.optionGroups, selected]);

  const total = basePrice + chosen.reduce((s, o) => s + o.priceDelta, 0);

  function toggle(groupId: string, optionId: string, single: boolean) {
    setSelected((prev) => {
      const cur = prev[groupId] ?? [];
      let next: string[];
      if (single) {
        next = [optionId];
      } else {
        next = cur.includes(optionId) ? cur.filter((x) => x !== optionId) : [...cur, optionId];
      }
      return { ...prev, [groupId]: next };
    });
  }

  function confirm() {
    for (const g of item.optionGroups) {
      const n = (selected[g.id] ?? []).length;
      if (n < g.minSelections) {
        setError(g.minSelections === 1 ? `Choose an option for "${g.name}".` : `Choose at least ${g.minSelections} for "${g.name}".`);
        return;
      }
      if (n > g.maxSelections) {
        setError(`Choose at most ${g.maxSelections} for "${g.name}".`);
        return;
      }
    }
    onConfirm(variant?.id ?? null, variant?.name ?? null, total, chosen);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-0 sm:items-center sm:p-4" onClick={onClose}>
      <div
        className="max-h-[85vh] w-full max-w-md overflow-y-auto rounded-t-2xl bg-white p-5 sm:rounded-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-3 flex items-start justify-between">
          <h2 className="text-lg font-bold">{item.name}</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        {item.variants.length > 0 && (
          <div className="mb-4">
            <div className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">Choose one · required</div>
            <div className="flex flex-col gap-1.5">
              {item.variants.map((v) => (
                <label
                  key={v.id}
                  className={`flex cursor-pointer items-center justify-between rounded-lg border px-3 py-2 text-sm ${
                    variant?.id === v.id ? "border-emerald-500 bg-emerald-50" : "border-slate-200"
                  }`}
                >
                  <span className="flex items-center gap-2">
                    <input type="radio" name="variant" checked={variant?.id === v.id} onChange={() => setVariant(v)} />
                    <span className="font-medium">{v.name}</span>
                  </span>
                  <span className="text-slate-600">{formatMoney(v.price, item.currency)}</span>
                </label>
              ))}
            </div>
          </div>
        )}

        {item.optionGroups.map((g) => {
          const single = g.maxSelections <= 1;
          const ids = selected[g.id] ?? [];
          return (
            <div key={g.id} className="mb-4">
              <div className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">
                {g.name} · {single ? "choose one" : "choose any"}
                {g.minSelections >= 1 ? " · required" : ""}
              </div>
              <div className="flex flex-col gap-1.5">
                {g.options.map((o) => (
                  <label
                    key={o.id}
                    className={`flex cursor-pointer items-center justify-between rounded-lg border px-3 py-2 text-sm ${
                      ids.includes(o.id) ? "border-emerald-500 bg-emerald-50" : "border-slate-200"
                    }`}
                  >
                    <span className="flex items-center gap-2">
                      <input
                        type={single ? "radio" : "checkbox"}
                        name={g.id}
                        checked={ids.includes(o.id)}
                        onChange={() => toggle(g.id, o.id, single)}
                      />
                      <span className="font-medium">{o.name}</span>
                    </span>
                    {o.priceDelta > 0 && <span className="text-emerald-700">+{formatMoney(o.priceDelta, item.currency)}</span>}
                  </label>
                ))}
              </div>
            </div>
          );
        })}

        {error && <div className="mb-3 rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</div>}

        <button
          type="button"
          onClick={confirm}
          className="w-full rounded-lg bg-emerald-600 px-4 py-2.5 text-center font-medium text-white hover:bg-emerald-700"
        >
          Add · {formatMoney(total, item.currency)}
        </button>
      </div>
    </div>
  );
}
