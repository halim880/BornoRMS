// DTOs for the Store / Warehouse screens (mirror the Blazor Store console pages:
// StoreDashboard / StoreItems / StoreCategories / StoreDepartments / StoreSuppliers /
// StoreGoodsReceipts / StoreRequisitions / StoreIssues / StoreLedger / StorePayables /
// StoreDepartmentIssues). JSON field names match the C# DTO property names (camelCase).
// Enums arrive as strings (the API registers JsonStringEnumConverter).

double _d(dynamic v) => v == null ? 0 : (v as num).toDouble();
double? _dOrNull(dynamic v) => v == null ? null : (v as num).toDouble();
int _i(dynamic v) => v == null ? 0 : (v as num).toInt();
String _s(dynamic v) => v?.toString() ?? '';
String? _sOrNull(dynamic v) => v?.toString();
DateTime _dt(dynamic v) => v == null ? DateTime.fromMillisecondsSinceEpoch(0) : DateTime.parse(v as String).toLocal();
DateTime? _dtOrNull(dynamic v) => v == null ? null : DateTime.parse(v as String).toLocal();

// ---------- dashboard ----------

/// Mirrors StoreDashboardSummaryDto.
class StoreDashboardSummary {
  final double totalStockValue;
  final int activeItemCount;
  final int lowStockItemCount;
  final int draftGrnCount;
  final int draftIssueCount;
  final String currency;

  StoreDashboardSummary({
    required this.totalStockValue,
    required this.activeItemCount,
    required this.lowStockItemCount,
    required this.draftGrnCount,
    required this.draftIssueCount,
    required this.currency,
  });

  factory StoreDashboardSummary.fromJson(Map<String, dynamic> j) => StoreDashboardSummary(
        totalStockValue: _d(j['totalStockValue']),
        activeItemCount: _i(j['activeItemCount']),
        lowStockItemCount: _i(j['lowStockItemCount']),
        draftGrnCount: _i(j['draftGrnCount']),
        draftIssueCount: _i(j['draftIssueCount']),
        currency: j['currency'] as String? ?? 'Tk',
      );
}

/// Mirrors StoreLowStockRow.
class StoreLowStockRow {
  final String itemId;
  final String code;
  final String name;
  final String unitCode;
  final double qtyOnHand;
  final double reorderLevel;
  final double reorderQty;
  final double stockValue;

  StoreLowStockRow({
    required this.itemId,
    required this.code,
    required this.name,
    required this.unitCode,
    required this.qtyOnHand,
    required this.reorderLevel,
    required this.reorderQty,
    required this.stockValue,
  });

  factory StoreLowStockRow.fromJson(Map<String, dynamic> j) => StoreLowStockRow(
        itemId: _s(j['itemId']),
        code: _s(j['code']),
        name: _s(j['name']),
        unitCode: _s(j['unitCode']),
        qtyOnHand: _d(j['qtyOnHand']),
        reorderLevel: _d(j['reorderLevel']),
        reorderQty: _d(j['reorderQty']),
        stockValue: _d(j['stockValue']),
      );
}

/// Aggregate /staff/store/dashboard payload: { summary, lowStock[] }.
class StoreDashboard {
  final StoreDashboardSummary summary;
  final List<StoreLowStockRow> lowStock;

  StoreDashboard({required this.summary, required this.lowStock});

  factory StoreDashboard.fromJson(Map<String, dynamic> j) => StoreDashboard(
        summary: StoreDashboardSummary.fromJson(j['summary'] as Map<String, dynamic>),
        lowStock: (j['lowStock'] as List? ?? [])
            .map((e) => StoreLowStockRow.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

// ---------- items ----------

/// Mirrors StoreItemDto.
class StoreItem {
  final String id;
  final String code;
  final String name;
  final String? banglaName;
  final String storeCategoryId;
  final String categoryName;
  final String baseUnitId;
  final String unitCode;
  final double qtyOnHand;
  final double reorderLevel;
  final double reorderQty;
  final double avgCost;
  final String currency;
  final bool isPerishable;
  final bool isActive;
  final double? packSize;
  final String? packNote;
  final bool isLowStock;
  final double stockValue;

  StoreItem({
    required this.id,
    required this.code,
    required this.name,
    required this.banglaName,
    required this.storeCategoryId,
    required this.categoryName,
    required this.baseUnitId,
    required this.unitCode,
    required this.qtyOnHand,
    required this.reorderLevel,
    required this.reorderQty,
    required this.avgCost,
    required this.currency,
    required this.isPerishable,
    required this.isActive,
    required this.packSize,
    required this.packNote,
    required this.isLowStock,
    required this.stockValue,
  });

  factory StoreItem.fromJson(Map<String, dynamic> j) => StoreItem(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        banglaName: _sOrNull(j['banglaName']),
        storeCategoryId: _s(j['storeCategoryId']),
        categoryName: _s(j['categoryName']),
        baseUnitId: _s(j['baseUnitId']),
        unitCode: _s(j['unitCode']),
        qtyOnHand: _d(j['qtyOnHand']),
        reorderLevel: _d(j['reorderLevel']),
        reorderQty: _d(j['reorderQty']),
        avgCost: _d(j['avgCost']),
        currency: j['currency'] as String? ?? 'Tk',
        isPerishable: j['isPerishable'] as bool? ?? false,
        isActive: j['isActive'] as bool? ?? false,
        packSize: _dOrNull(j['packSize']),
        packNote: _sOrNull(j['packNote']),
        isLowStock: j['isLowStock'] as bool? ?? false,
        stockValue: _d(j['stockValue']),
      );
}

// ---------- categories ----------

/// Mirrors StoreCategoryDto.
class StoreCategory {
  final String id;
  final String name;
  final String? banglaName;
  final String? description;
  final int displayOrder;
  final bool isActive;

  StoreCategory({
    required this.id,
    required this.name,
    required this.banglaName,
    required this.description,
    required this.displayOrder,
    required this.isActive,
  });

  factory StoreCategory.fromJson(Map<String, dynamic> j) => StoreCategory(
        id: _s(j['id']),
        name: _s(j['name']),
        banglaName: _sOrNull(j['banglaName']),
        description: _sOrNull(j['description']),
        displayOrder: _i(j['displayOrder']),
        isActive: j['isActive'] as bool? ?? false,
      );
}

// ---------- departments ----------

/// Mirrors StoreDepartmentDto.
class StoreDepartment {
  final String id;
  final String code;
  final String name;
  final String? banglaName;
  final String? description;
  final int displayOrder;
  final bool isActive;

  StoreDepartment({
    required this.id,
    required this.code,
    required this.name,
    required this.banglaName,
    required this.description,
    required this.displayOrder,
    required this.isActive,
  });

  factory StoreDepartment.fromJson(Map<String, dynamic> j) => StoreDepartment(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        banglaName: _sOrNull(j['banglaName']),
        description: _sOrNull(j['description']),
        displayOrder: _i(j['displayOrder']),
        isActive: j['isActive'] as bool? ?? false,
      );
}

// ---------- suppliers ----------

/// Mirrors StoreSupplierDto.
class StoreSupplier {
  final String id;
  final String code;
  final String name;
  final String? phone;
  final String? address;
  final int paymentTermsDays;
  final String? notes;
  final bool isActive;

  StoreSupplier({
    required this.id,
    required this.code,
    required this.name,
    required this.phone,
    required this.address,
    required this.paymentTermsDays,
    required this.notes,
    required this.isActive,
  });

  factory StoreSupplier.fromJson(Map<String, dynamic> j) => StoreSupplier(
        id: _s(j['id']),
        code: _s(j['code']),
        name: _s(j['name']),
        phone: _sOrNull(j['phone']),
        address: _sOrNull(j['address']),
        paymentTermsDays: _i(j['paymentTermsDays']),
        notes: _sOrNull(j['notes']),
        isActive: j['isActive'] as bool? ?? false,
      );
}

// ---------- goods receipts (GRN) ----------

/// Mirrors StoreGoodsReceiptListItemDto.
class StoreGoodsReceipt {
  final String id;
  final String grnNumber;
  final String storeSupplierId;
  final String supplierName;
  final String? invoiceNo;
  final DateTime receivedAtUtc;
  final String currency;
  final String status;
  final int lineCount;
  final double subtotal;

  StoreGoodsReceipt({
    required this.id,
    required this.grnNumber,
    required this.storeSupplierId,
    required this.supplierName,
    required this.invoiceNo,
    required this.receivedAtUtc,
    required this.currency,
    required this.status,
    required this.lineCount,
    required this.subtotal,
  });

  factory StoreGoodsReceipt.fromJson(Map<String, dynamic> j) => StoreGoodsReceipt(
        id: _s(j['id']),
        grnNumber: _s(j['grnNumber']),
        storeSupplierId: _s(j['storeSupplierId']),
        supplierName: _s(j['supplierName']),
        invoiceNo: _sOrNull(j['invoiceNo']),
        receivedAtUtc: _dt(j['receivedAtUtc']),
        currency: j['currency'] as String? ?? 'Tk',
        status: _s(j['status']),
        lineCount: _i(j['lineCount']),
        subtotal: _d(j['subtotal']),
      );
}

// ---------- requisitions ----------

/// Mirrors StoreRequisitionListItemDto.
class StoreRequisition {
  final String id;
  final String requisitionNumber;
  final String storeDepartmentId;
  final String departmentName;
  final DateTime requestedAtUtc;
  final DateTime? requiredByUtc;
  final String status;
  final int lineCount;

  StoreRequisition({
    required this.id,
    required this.requisitionNumber,
    required this.storeDepartmentId,
    required this.departmentName,
    required this.requestedAtUtc,
    required this.requiredByUtc,
    required this.status,
    required this.lineCount,
  });

  factory StoreRequisition.fromJson(Map<String, dynamic> j) => StoreRequisition(
        id: _s(j['id']),
        requisitionNumber: _s(j['requisitionNumber']),
        storeDepartmentId: _s(j['storeDepartmentId']),
        departmentName: _s(j['departmentName']),
        requestedAtUtc: _dt(j['requestedAtUtc']),
        requiredByUtc: _dtOrNull(j['requiredByUtc']),
        status: _s(j['status']),
        lineCount: _i(j['lineCount']),
      );
}

// ---------- issues ----------

/// Mirrors StoreIssueListItemDto.
class StoreIssue {
  final String id;
  final String issueNumber;
  final String storeDepartmentId;
  final String destination;
  final DateTime issuedAtUtc;
  final String status;
  final int lineCount;
  final double totalQtyBase;
  final String? storeRequisitionId;
  final String? requisitionNumber;

  StoreIssue({
    required this.id,
    required this.issueNumber,
    required this.storeDepartmentId,
    required this.destination,
    required this.issuedAtUtc,
    required this.status,
    required this.lineCount,
    required this.totalQtyBase,
    required this.storeRequisitionId,
    required this.requisitionNumber,
  });

  factory StoreIssue.fromJson(Map<String, dynamic> j) => StoreIssue(
        id: _s(j['id']),
        issueNumber: _s(j['issueNumber']),
        storeDepartmentId: _s(j['storeDepartmentId']),
        destination: _s(j['destination']),
        issuedAtUtc: _dt(j['issuedAtUtc']),
        status: _s(j['status']),
        lineCount: _i(j['lineCount']),
        totalQtyBase: _d(j['totalQtyBase']),
        storeRequisitionId: _sOrNull(j['storeRequisitionId']),
        requisitionNumber: _sOrNull(j['requisitionNumber']),
      );
}

// ---------- movement ledger ----------

/// Mirrors StoreMovementRow.
class StoreMovementRow {
  final DateTime occurredAtUtc;
  final String itemName;
  final String unitCode;
  final String movementType;
  final double qtyBase;
  final double unitCost;
  final String? reason;
  final String? referenceType;
  final double? runningBalance;

  StoreMovementRow({
    required this.occurredAtUtc,
    required this.itemName,
    required this.unitCode,
    required this.movementType,
    required this.qtyBase,
    required this.unitCost,
    required this.reason,
    required this.referenceType,
    required this.runningBalance,
  });

  factory StoreMovementRow.fromJson(Map<String, dynamic> j) => StoreMovementRow(
        occurredAtUtc: _dt(j['occurredAtUtc']),
        itemName: _s(j['itemName']),
        unitCode: _s(j['unitCode']),
        movementType: _s(j['movementType']),
        qtyBase: _d(j['qtyBase']),
        unitCost: _d(j['unitCost']),
        reason: _sOrNull(j['reason']),
        referenceType: _sOrNull(j['referenceType']),
        runningBalance: _dOrNull(j['runningBalance']),
      );
}

/// Mirrors StoreMovementLedgerDto.
class StoreMovementLedger {
  final String? itemId;
  final String? itemName;
  final String? unitCode;
  final DateTime? fromUtc;
  final DateTime? toUtc;
  final double? openingBalance;
  final double? closingBalance;
  final List<StoreMovementRow> rows;

  StoreMovementLedger({
    required this.itemId,
    required this.itemName,
    required this.unitCode,
    required this.fromUtc,
    required this.toUtc,
    required this.openingBalance,
    required this.closingBalance,
    required this.rows,
  });

  factory StoreMovementLedger.fromJson(Map<String, dynamic> j) => StoreMovementLedger(
        itemId: _sOrNull(j['itemId']),
        itemName: _sOrNull(j['itemName']),
        unitCode: _sOrNull(j['unitCode']),
        fromUtc: _dtOrNull(j['fromUtc']),
        toUtc: _dtOrNull(j['toUtc']),
        openingBalance: _dOrNull(j['openingBalance']),
        closingBalance: _dOrNull(j['closingBalance']),
        rows: (j['rows'] as List? ?? [])
            .map((e) => StoreMovementRow.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

// ---------- supplier payables ----------

/// Mirrors StoreSupplierPayableDto.
class StoreSupplierPayable {
  final String supplierId;
  final String code;
  final String name;
  final String? phone;
  final int paymentTermsDays;
  final double billed;
  final double paid;
  final double outstanding;

  StoreSupplierPayable({
    required this.supplierId,
    required this.code,
    required this.name,
    required this.phone,
    required this.paymentTermsDays,
    required this.billed,
    required this.paid,
    required this.outstanding,
  });

  factory StoreSupplierPayable.fromJson(Map<String, dynamic> j) => StoreSupplierPayable(
        supplierId: _s(j['supplierId']),
        code: _s(j['code']),
        name: _s(j['name']),
        phone: _sOrNull(j['phone']),
        paymentTermsDays: _i(j['paymentTermsDays']),
        billed: _d(j['billed']),
        paid: _d(j['paid']),
        outstanding: _d(j['outstanding']),
      );
}

// ---------- department consumption report ----------

/// Mirrors StoreDepartmentConsumptionItemDto.
class StoreDepartmentConsumptionItem {
  final String storeItemId;
  final String itemName;
  final String baseUnitCode;
  final double qtyBase;
  final double value;

  StoreDepartmentConsumptionItem({
    required this.storeItemId,
    required this.itemName,
    required this.baseUnitCode,
    required this.qtyBase,
    required this.value,
  });

  factory StoreDepartmentConsumptionItem.fromJson(Map<String, dynamic> j) =>
      StoreDepartmentConsumptionItem(
        storeItemId: _s(j['storeItemId']),
        itemName: _s(j['itemName']),
        baseUnitCode: _s(j['baseUnitCode']),
        qtyBase: _d(j['qtyBase']),
        value: _d(j['value']),
      );
}

/// Mirrors StoreDepartmentConsumptionRowDto.
class StoreDepartmentConsumptionRow {
  final String storeDepartmentId;
  final String departmentName;
  final double totalQtyBase;
  final double totalValue;
  final List<StoreDepartmentConsumptionItem> items;

  StoreDepartmentConsumptionRow({
    required this.storeDepartmentId,
    required this.departmentName,
    required this.totalQtyBase,
    required this.totalValue,
    required this.items,
  });

  factory StoreDepartmentConsumptionRow.fromJson(Map<String, dynamic> j) =>
      StoreDepartmentConsumptionRow(
        storeDepartmentId: _s(j['storeDepartmentId']),
        departmentName: _s(j['departmentName']),
        totalQtyBase: _d(j['totalQtyBase']),
        totalValue: _d(j['totalValue']),
        items: (j['items'] as List? ?? [])
            .map((e) => StoreDepartmentConsumptionItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Mirrors StoreDepartmentConsumptionResultDto.
class StoreDepartmentConsumption {
  final DateTime fromUtc;
  final DateTime toUtc;
  final double grandTotalValue;
  final List<StoreDepartmentConsumptionRow> rows;

  StoreDepartmentConsumption({
    required this.fromUtc,
    required this.toUtc,
    required this.grandTotalValue,
    required this.rows,
  });

  factory StoreDepartmentConsumption.fromJson(Map<String, dynamic> j) => StoreDepartmentConsumption(
        fromUtc: _dt(j['fromUtc']),
        toUtc: _dt(j['toUtc']),
        grandTotalValue: _d(j['grandTotalValue']),
        rows: (j['rows'] as List? ?? [])
            .map((e) => StoreDepartmentConsumptionRow.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
