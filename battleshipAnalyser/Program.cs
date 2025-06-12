
using System.Drawing;
using ScottPlot;

class Program
{
    public static void Main()
    {

        //I want the following plots
        //horsepower/tonnes per year, different plot for each nation and type (BB, B, BC)

        Dictionary<string, List<(double, double, double, double)>> HorsepowerPerTonne_each_year = new();

        foreach (var line in File.ReadAllLines("ships_normal_power.csv"))
        {
            var tokens = line.Split(',', StringSplitOptions.TrimEntries);
            //Skip the header
            if (tokens[0] == "name")
            {
                continue;
            }
            else
            {
                //The format must be the same as that we print as
                if (!int.TryParse(tokens[6], out int year))
                    continue;
                if (!double.TryParse(tokens[5], out double speed) || speed == 0)
                    continue;
                if (!double.TryParse(tokens[7], out double powerToWeight) || powerToWeight == 0)
                    continue;
                if (!double.TryParse(tokens[3], out double displacement) || displacement== 0)
                    continue;

                string navy = tokens[2];
                string type = tokens[8];

                string plot_name = navy + " " + type;
                if (!HorsepowerPerTonne_each_year.ContainsKey(plot_name))
                    HorsepowerPerTonne_each_year[plot_name] = new();
                HorsepowerPerTonne_each_year[plot_name].Add((year, speed, powerToWeight,displacement));
            }
        }

        //Some colours I think makes sense for the navies, mainly based on their flag
        var Colors = new Dictionary<string,ScottPlot.Color>();

        Colors["Chile BB"] = ScottPlot.Color.FromARGB(0xFFFF0000);
        Colors["Kingdom of Italy BB"] = ScottPlot.Color.FromARGB(0xFF00FF00);
        Colors["Russian Empire BB"] = ScottPlot.Color.FromARGB(0xFFAAFFAA);
        Colors["German Empire BB"] = ScottPlot.Color.FromARGB(0xFFAAAAAA);
        Colors["United Kingdom BB"] = ScottPlot.Color.FromARGB(0xFFFF5454);
        Colors["Nazi Germany BB"] = ScottPlot.Color.FromARGB(0xFF303030);
        Colors["France BB"] = ScottPlot.Color.FromARGB(0xFF0000FF);
        Colors["United States BB"] = ScottPlot.Color.FromARGB(0xFF6060FF);
        Colors["Austria-Hungary BB"] = ScottPlot.Color.FromARGB(0xFFFFFF00);
        Colors["Spain BB"] = ScottPlot.Color.FromARGB(0xFFFFB600);
        Colors["Empire of Japan BB"] = ScottPlot.Color.FromARGB(0xFFFFBFBF);
        Colors["Brazil BB"] = ScottPlot.Color.FromARGB(0xFFAB4e00);
        Colors["Argentina BB"] = ScottPlot.Color.FromARGB(0xFFAAAAFF);
        Colors["United Kingdom BC"] = Colors["United Kingdom BB"];
        Colors["German Empire BC"] = Colors["German Empire BB"];
        Colors["Nazi Germany BC"] = Colors["Nazi Germany BB"];
        Colors["Netherlands BC"] = ScottPlot.Color.FromARGB(0xFFFF9300);
        Colors["Australia BC"] = Colors["United Kingdom BB"];
        Colors["Empire of Japan BC"] = Colors["Empire of Japan BB"];
        Colors["Russian Empire BC"] = Colors["Russian Empire BB"];
        Colors["Soviet Union BC"] = ScottPlot.Color.FromARGB(0xFFC00000);
        Colors["United States BC"] = Colors["United States BB"];

        LegendItem[] legends = Colors.Select(x =>
        {
            return new LegendItem
            {
                LineColor = x.Value,
                LabelText = x.Key,
                LineWidth = 4,
                MarkerFillColor = x.Value,
                MarkerLineColor = x.Value,
                LinePattern = x.Key.EndsWith("BC") ? ScottPlot.LinePattern.Dashed : ScottPlot.LinePattern.Solid,
            };
        }).ToArray();

        foreach (var plot in HorsepowerPerTonne_each_year)
        {
            HorsepowerPerTonne_each_year[plot.Key] = plot.Value.OrderBy(x => x.Item1).ToList();
        }

        ScottPlot.Plot allPlot_per_year = new();
        ScottPlot.Plot allPlot_knot_per_year = new();
        ScottPlot.Plot allPlot_per_knot = new();
        ScottPlot.Plot allPlot_per_displacement = new();
        ScottPlot.Plot allPlot_hp_per_displacement = new();
        foreach (var plot in HorsepowerPerTonne_each_year)
        {
            {
                double[] xs = plot.Value.Select(x => x.Item1).ToArray();
                double[] ys = plot.Value.Select(x => x.Item3).ToArray();
                var line = allPlot_per_year.Add.Scatter(xs, ys,Colors[plot.Key]);
                line.LinePattern = plot.Key.EndsWith("BC") ? ScottPlot.LinePattern.Dashed : ScottPlot.LinePattern.Solid;
                ScottPlot.Plot thisPlot_per_year = new();
                thisPlot_per_year.Add.Scatter(xs, ys);
                thisPlot_per_year.SaveSvg("old/hpperyear/" + plot.Key + ".svg", 800, 300);
            }
        }
        foreach (var plot in HorsepowerPerTonne_each_year)
        {
            {
                double[] xs = plot.Value.Select(x => x.Item1).ToArray();
                double[] ys = plot.Value.Select(x => x.Item2).ToArray();
                var line = allPlot_knot_per_year.Add.Scatter(xs, ys,Colors[plot.Key]);
                line.LinePattern = plot.Key.EndsWith("BC") ? ScottPlot.LinePattern.Dashed : ScottPlot.LinePattern.Solid;
                ScottPlot.Plot thisPlot_per_year = new();
                thisPlot_per_year.Add.Scatter(xs, ys);
                thisPlot_per_year.SaveSvg("old/knotperyear/" + plot.Key + ".svg", 800, 300);
            }
        }
        foreach (var plot in HorsepowerPerTonne_each_year)
        {
            HorsepowerPerTonne_each_year[plot.Key] = plot.Value.OrderBy(x => x.Item2).ToList();
        }
        foreach (var plot in HorsepowerPerTonne_each_year)
        {
            {
                double[] xs = plot.Value.Select(x => x.Item2).ToArray();
                double[] ys = plot.Value.Select(x => x.Item3).ToArray();
                var line = allPlot_per_knot.Add.Scatter(xs, ys,Colors[plot.Key]);
                line.LinePattern = plot.Key.EndsWith("BC") ? ScottPlot.LinePattern.Dashed : ScottPlot.LinePattern.Solid;
                ScottPlot.Plot thisPlot_per_knot = new();
                thisPlot_per_knot.Add.Scatter(xs, ys);
                thisPlot_per_knot.SaveSvg("old/hpperknot/" + plot.Key + ".svg", 800, 300);
            }

        }
//        allPlot_knot_per_year.ShowLegend(legends);
        allPlot_knot_per_year.SaveSvg("old/knotperyear/AllShips.svg", 800, 300);
//        allPlot_per_year.ShowLegend(legends);
        allPlot_per_year.SaveSvg("old/hpperyear/AllShips.svg", 800, 300);
//        allPlot_per_knot.ShowLegend(legends);
        allPlot_per_knot.SaveSvg("old/hpperknot/AllShips.svg", 800, 300);

        foreach (var plot in HorsepowerPerTonne_each_year)
        {
            HorsepowerPerTonne_each_year[plot.Key] = plot.Value.OrderBy(x => x.Item4).ToList();
        }
        foreach (var plot in HorsepowerPerTonne_each_year)
        {
            {
                double[] xs = plot.Value.Select(x => x.Item4).ToArray();
                double[] ys = plot.Value.Select(x => x.Item3).ToArray();
                var line = allPlot_per_displacement.Add.Scatter(xs, ys,Colors[plot.Key]);
                line.LinePattern = plot.Key.EndsWith("BC") ? ScottPlot.LinePattern.Dashed : ScottPlot.LinePattern.Solid;
                ScottPlot.Plot thisPlot_per_displ = new();
                thisPlot_per_displ.Add.Scatter(xs, ys);
                thisPlot_per_displ.SaveSvg("old/hpperdisplacement/" + plot.Key + ".svg", 800, 300);
            }
        }
        //allPlot_per_displacement.ShowLegend(legends);
        allPlot_per_displacement.SaveSvg("old/hpperdisplacement/AllShips.svg", 800, 300);

        foreach (var plot in HorsepowerPerTonne_each_year)
        {
            {
                double[] xs = plot.Value.Select(x => x.Item4).ToArray();
                double[] ys = plot.Value.Select(x => x.Item2).ToArray();
                var line = allPlot_hp_per_displacement.Add.Scatter(xs, ys, Colors[plot.Key]);
                line.LinePattern = plot.Key.EndsWith("BC") ? ScottPlot.LinePattern.Dashed : ScottPlot.LinePattern.Solid;
                ScottPlot.Plot thisPlot_per_displ = new();
                thisPlot_per_displ.Add.Scatter(xs, ys);
                thisPlot_per_displ.SaveSvg("old/plain_hpperdisplacement/" + plot.Key + ".svg", 800, 300);
            }
        }
        //allPlot_per_displacement.ShowLegend(legends);
        allPlot_hp_per_displacement.SaveSvg("old/plain_hpperdisplacement/AllShips.svg", 800, 300);


        Console.WriteLine("\n\n");
    }
}