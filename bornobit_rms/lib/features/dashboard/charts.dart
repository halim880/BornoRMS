import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../../core/models/dtos.dart';
import '../../core/theme/app_theme.dart';
import 'widgets.dart';

const _palette = [
  Bo.primary,
  Bo.accent,
  Bo.info,
  Bo.success,
  Bo.warning,
  Bo.danger,
  Bo.primaryStrong,
  Bo.slate400,
];

/// Section-3 line chart: paid revenue across the 24 hours.
class SalesByHourChart extends StatelessWidget {
  final List<HourlySales> data;
  const SalesByHourChart({super.key, required this.data});

  @override
  Widget build(BuildContext context) {
    if (data.isEmpty) return const _Empty();
    final maxY = data.map((e) => e.revenue).fold<double>(0, (a, b) => b > a ? b : a);
    final spots = data.map((e) => FlSpot(e.hour.toDouble(), e.revenue)).toList();

    return SizedBox(
      height: 200,
      child: LineChart(
        LineChartData(
          minY: 0,
          maxY: maxY <= 0 ? 1 : maxY * 1.2,
          gridData: const FlGridData(show: true, drawVerticalLine: false),
          borderData: FlBorderData(show: false),
          titlesData: FlTitlesData(
            topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            leftTitles: const AxisTitles(sideTitles: SideTitles(showTitles: true, reservedSize: 44)),
            bottomTitles: AxisTitles(
              sideTitles: SideTitles(
                showTitles: true,
                interval: 3,
                getTitlesWidget: (v, _) => Padding(
                  padding: const EdgeInsets.only(top: 6),
                  child: Text('${v.toInt()}h', style: const TextStyle(fontSize: 10, color: Bo.textSubtle)),
                ),
              ),
            ),
          ),
          lineBarsData: [
            LineChartBarData(
              spots: spots,
              isCurved: true,
              color: Bo.primary,
              barWidth: 2.5,
              dotData: const FlDotData(show: false),
              belowBarData: BarAreaData(show: true, color: Bo.primary.withValues(alpha: 0.12)),
            ),
          ],
        ),
      ),
    );
  }
}

/// Section-3 pie chart: paid revenue by category.
class SalesByCategoryChart extends StatelessWidget {
  final List<CategorySales> data;
  const SalesByCategoryChart({super.key, required this.data});

  @override
  Widget build(BuildContext context) {
    if (data.isEmpty) return const _Empty();
    final total = data.fold<double>(0, (a, b) => a + b.revenue);

    return SizedBox(
      height: 200,
      child: Row(
        children: [
          Expanded(
            flex: 3,
            child: PieChart(
              PieChartData(
                sectionsSpace: 2,
                centerSpaceRadius: 36,
                sections: [
                  for (var i = 0; i < data.length; i++)
                    PieChartSectionData(
                      value: data[i].revenue,
                      color: _palette[i % _palette.length],
                      radius: 56,
                      title: total <= 0 ? '' : '${(data[i].revenue / total * 100).round()}%',
                      titleStyle: const TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.w700),
                    ),
                ],
              ),
            ),
          ),
          Expanded(
            flex: 2,
            child: ListView(
              children: [
                for (var i = 0; i < data.length; i++)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 3),
                    child: Row(
                      children: [
                        Container(width: 10, height: 10, color: _palette[i % _palette.length]),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(data[i].category,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(fontSize: 12, color: Bo.textMuted)),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Section-3 top items: a ranked list with proportional bars.
class TopItemsChart extends StatelessWidget {
  final List<TopItemRow> data;
  const TopItemsChart({super.key, required this.data});

  @override
  Widget build(BuildContext context) {
    if (data.isEmpty) return const _Empty();
    final maxQty = data.map((e) => e.quantitySold).fold<int>(0, (a, b) => b > a ? b : a);

    return Column(
      children: [
        for (final item in data)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 4),
            child: Row(
              children: [
                Expanded(
                  flex: 4,
                  child: Text(item.name,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 12, color: Bo.text)),
                ),
                Expanded(
                  flex: 5,
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(Bo.radiusSm),
                    child: LinearProgressIndicator(
                      value: maxQty <= 0 ? 0 : item.quantitySold / maxQty,
                      minHeight: 14,
                      backgroundColor: Bo.slate100,
                      valueColor: const AlwaysStoppedAnimation(Bo.primary),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                SizedBox(
                  width: 40,
                  child: Text(count(item.quantitySold),
                      textAlign: TextAlign.right,
                      style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: Bo.text)),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

class _Empty extends StatelessWidget {
  const _Empty();
  @override
  Widget build(BuildContext context) => const SizedBox(
        height: 120,
        child: Center(child: Text('No data for this range', style: TextStyle(color: Bo.textSubtle))),
      );
}
