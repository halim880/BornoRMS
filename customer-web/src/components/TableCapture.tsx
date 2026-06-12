"use client";

import { useEffect } from "react";
import { writeTable, type TableInfo } from "@/lib/table";

export default function TableCapture({ table }: { table: TableInfo | null }) {
  useEffect(() => {
    if (table) writeTable(table);
  }, [table]);

  return null;
}
