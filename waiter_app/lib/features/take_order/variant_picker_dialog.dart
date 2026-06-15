import 'package:flutter/material.dart';

import '../../core/models/dtos.dart';
import '../../core/widgets/format.dart';

Future<ProductVariant?> pickVariant(BuildContext context, Product product) {
  return showModalBottomSheet<ProductVariant>(
    context: context,
    showDragHandle: true,
    builder: (ctx) => SafeArea(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
            child: Text('Choose size · ${product.name}',
                style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          ),
          const Divider(height: 1),
          ...product.variants.map((v) => ListTile(
                title: Text(v.name),
                trailing: Text(money(v.price, currency: product.currency, twoDp: true)),
                onTap: () => Navigator.pop(ctx, v),
              )),
        ],
      ),
    ),
  );
}
