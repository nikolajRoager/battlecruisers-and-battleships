using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

public class Ship
{
    /// <summary>
    /// Name, on Wikipedia, including disambiguation, so the wikipage is
    /// https://en.wikipedia.org/wiki/{Name}
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Alternative name to use as a fallback, if we can't get the article from the first
    /// </summary>
    public string AltName { get; set; }
    /// <summary>
    /// Date of commision
    /// </summary>
    public DateOnly LaunchDate { get; set; }
    /// <summary>
    /// Name of the class on Wikipedia
    /// </summary>
    public string ClassName { get; set; }
    /// <summary>
    /// Type of the ship
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// Navy which operates the ship
    /// </summary>
    public string Navy { get; set; }


    public enum Displacement
    {
        Standard,
        Normal,
        Deep,
    }

    /// <summary>
    /// Displacement in metric tons, preferably normal or standard displacement
    /// </summary>
    public Dictionary<Displacement, double> displacementMT { get; set; }
    /// <summary>
    /// Power in shaft horsepower, if false we only have indicated horsepower
    /// </summary>
    public Dictionary<bool, double> powerShp { get; set; }

    /// <summary>
    /// Speed in knots
    /// </summary>
    public double speedKn { get; set; }

    public Ship(string name, string altName, string launchDate, string className, string type, string navy)
    {
        powerShp = new();
        displacementMT = new();
        speedKn = 0;
        Name = name;
        AltName = altName;
        //The Wikipedia dates are
        try
        {
            LaunchDate = DateOnly.ParseExact(launchDate, ["yyyy-M-d", "yyyy-MM-d", "yyyy-M-dd", "yyyy-MM-dd"], System.Globalization.CultureInfo.InvariantCulture);
        }
        //At the time of writing, there is an error on Wikipedia, the launch date of USS Idaho is written as 1905-21-09, which is either wrong format (as per Wikipedia guidelines) or a typo of 1905-12-09
        //I am looking into it, but before fixing it
        //(Nevermind someone else fixed it already, I am keeping this comment)
        catch (Exception ex)
        {
            //I think the correct date for Idaho is 1905-12-09, but I am not sure, so I default to january 1st of the year
            if (!int.TryParse(launchDate.Substring(0, 4), out int year))
            {
                throw new ArgumentException("Problem with date format for " + name + " : " + ex.Message + "; could not even find year!");
            }
            LaunchDate = new DateOnly(year, 1, 1);
            Console.WriteLine("Problem with date format for " + name + " : " + ex.Message + " could not read as yyyy-MM-dd defaulting to " + LaunchDate.ToString());
        }
        ClassName = className;
        Type = type;
        Navy = navy;
    }

    /// <summary>
    /// Get all stats from the infobox in the Wikipedia article
    /// </summary>
    /// <returns></returns>
    public async Task DownloadData()
    {
        Console.WriteLine($"{Name}");
        Regex extractInfobox = new Regex(@"{{Infobox\sship\scharacteristics\n([\|\*\n].*?\n)+");

        //I have some examples of displacement in the file displacements.wiki, you can try out this regex on that
        Regex getDisplacement = new Regex(
            @"[\*=].*?((\(\[\[(?<typeBefore>.*?)\]\]\)|\[\[.*?\]\]|\(.*?\)|.*?):?)?\s?{{(convert|cvt)\|((?<number1>\d+(,\d+)?)\|to\|\d+(,\d+)?|(?<number2>\d+(,\d+)?)+-\d+(,\d+)?|(?<number3>\d+(,\d+)?))\|(?<from>t(?!o)|LT|ST|MT)\|?((?<to>t|LT|ST|MT)\|)?.+?(\|lk=on)?}}*.?(\(\[\[(?<typeAfter0>.*?)\]\]\)|\[\[(?<typeAfter1>.*?)\]\]|\((?<typeAfter2>.*?)\)|(?<typeAfter3>.*?))?");
        //This is essentially a simpler version of the above regex
        Regex getPower= new Regex(
            @"[\*=].*?{{(convert|cvt)\|((?<number1>\d+(,\d+)?)\|to\|\d+(,\d+)?|(?<number2>\d+(,\d+)?)-\d+(,\d+)?|(?<number3>\d+(,\d+)?))\|(?<from>shp|ihp|kW|PS)");

        Regex getSpeed = new Regex(
            @"[\*=].*?{{(convert|cvt)\|((?<number1>\d+(,\d+)?)\|to\|\d+|(?<number2>\d+(,\d+)?)-\d+|(?<number3>\d+(,\d+)?)).*?\|(?<from>kn|knot|knots)");

        Regex getNormalDisplacement = new Regex(@"(normal)",RegexOptions.IgnoreCase);
        Regex getStandardDisplacement = new Regex(@"(standard|treaty)",RegexOptions.IgnoreCase);
        Regex getDeepDisplacement = new Regex(@"(deep|full|load)",RegexOptions.IgnoreCase);

        
        //The same semi-AI generated code as before to download the article for that ship
        //Again, comments and error handling is human generated
        string url = $"https://en.wikipedia.org/w/api.php?action=query&prop=revisions&rvprop=content&format=json&titles={Name}";

        using HttpClient client = new HttpClient();
        string response = await client.GetStringAsync(url);
        JObject json = JObject.Parse(response);

        //Assume the json looks like I expect, but do check for errors
        var pages = json["query"]?["pages"];
        if (pages == null)
        {
            return;
        }
        string infobox = "";
        //My guess is that there will only be one page, but you never know, just loop through and get the infobox
        foreach (var page in pages)
        {
            //It is not null, I know it is not null, but the compiler doesn't
            string? wikitext = (string?)page?.First?["revisions"]?[0]?["*"];
            if (wikitext == null)
                continue;
            //The infobox contains all info worth looking at
            var infoboxMatch = extractInfobox.Match(wikitext);
            if (infoboxMatch.Success)
            {
                infobox = infoboxMatch.Groups[0].Value;
                break;
            }
        }
        //Well ... crap, try the alternate name
        if (infobox.Length == 0)
        {

            //The same semi-AI generated code as before to download the article for that ship
            //Again, comments and error handling is human generated
            url = $"https://en.wikipedia.org/w/api.php?action=query&prop=revisions&rvprop=content&format=json&titles={AltName}";

            response = await client.GetStringAsync(url);
            json = JObject.Parse(response);

            //Assume the json looks like I expect, but do check for errors
            pages = json["query"]?["pages"];
            if (pages == null)
            {
                return;
            }
            infobox = "";
            //My guess is that there will only be one page, but you never know, just loop through and get the infobox
            foreach (var page in pages)
            {
                //It is not null, I know it is not null, but the compiler doesn't
                string? wikitext = (string?)page?.First?["revisions"]?[0]?["*"];
                if (wikitext == null)
                    continue;
                //The infobox contains all info worth looking at
                var infoboxMatch = extractInfobox.Match(wikitext);
                if (infoboxMatch.Success)
                {
                    infobox = infoboxMatch.Groups[0].Value;
                    break;
                }
            }
        }
        //ohh shiit... try alt_name_(year)
        if (infobox.Length == 0)
        {

            //The same semi-AI generated code as before to download the article for that ship
            //Again, comments and error handling is human generated
            url = $"https://en.wikipedia.org/w/api.php?action=query&prop=revisions&rvprop=content&format=json&titles={AltName}_({LaunchDate.Year})";

            response = await client.GetStringAsync(url);
            json = JObject.Parse(response);

            //Assume the json looks like I expect, but do check for errors
            pages = json["query"]?["pages"];
            if (pages == null)
            {
                return;
            }
            infobox = "";
            //My guess is that there will only be one page, but you never know, just loop through and get the infobox
            foreach (var page in pages)
            {
                //It is not null, I know it is not null, but the compiler doesn't
                string? wikitext = (string?)page?.First?["revisions"]?[0]?["*"];
                if (wikitext == null)
                    continue;
                //The infobox contains all info worth looking at
                var infoboxMatch = extractInfobox.Match(wikitext);
                if (infoboxMatch.Success)
                {
                    infobox = infoboxMatch.Groups[0].Value;
                    break;
                }
            }
        }
        {
            {
                //Extract displacement
                var displacementMatch = getDisplacement.Match(infobox);
                //Loop through all displacements, 
                for (; displacementMatch.Success; displacementMatch = displacementMatch.NextMatch())
                    if (displacementMatch.Success)
                    {
                        Displacement displacementType = Displacement.Normal;

                        string displacementString = displacementMatch.Groups[0].Value;

                        if (getNormalDisplacement.IsMatch(displacementString))
                            displacementType = Displacement.Normal;
                        else if (getStandardDisplacement.IsMatch(displacementString))
                            displacementType = Displacement.Standard;
                        else if (getDeepDisplacement.IsMatch(displacementString))
                            displacementType = Displacement.Deep;

                        double mass = 0;
                        //One of these are going to be found
                        if (!string.IsNullOrEmpty(displacementMatch.Groups["number0"].Value))
                            mass = double.Parse(displacementMatch.Groups["number0"].Value);
                        else if (!string.IsNullOrEmpty(displacementMatch.Groups["number1"].Value))
                            mass = double.Parse(displacementMatch.Groups["number1"].Value);
                        else if (!string.IsNullOrEmpty(displacementMatch.Groups["number2"].Value))
                            mass = double.Parse(displacementMatch.Groups["number2"].Value);
                        else if (!string.IsNullOrEmpty(displacementMatch.Groups["number3"].Value))
                            mass = double.Parse(displacementMatch.Groups["number3"].Value);

                        //Metric tonnes
                        if (displacementMatch.Groups["from"].Value.ToLower() == "mt")
                            displacementMT[displacementType] = mass;
                        else if (displacementMatch.Groups["from"].Value.ToLower() == "t")
                            displacementMT[displacementType] = mass;
                        //British tonnes
                        else if (displacementMatch.Groups["from"].Value.ToLower() == "lt")
                            displacementMT[displacementType] = mass * 1.016;
                        //US American tonnes
                        else if (displacementMatch.Groups["from"].Value.ToLower() == "st")
                            displacementMT[displacementType] = mass * 0.90718474;
                    }
                //Extract power
                var powerMatch = getPower.Match(infobox);
                //Loop through all displacements, 
                for (; powerMatch.Success; powerMatch = powerMatch.NextMatch())
                    if (powerMatch.Success)
                    {


                        double power = 0;
                        //One of these are going to be found
                        if (!string.IsNullOrEmpty(powerMatch.Groups["number0"].Value))
                            power = double.Parse(powerMatch.Groups["number0"].Value);
                        else if (!string.IsNullOrEmpty(powerMatch.Groups["number1"].Value))
                            power = double.Parse(powerMatch.Groups["number1"].Value);
                        else if (!string.IsNullOrEmpty(powerMatch.Groups["number2"].Value))
                            power = double.Parse(powerMatch.Groups["number2"].Value);
                        else if (!string.IsNullOrEmpty(powerMatch.Groups["number3"].Value))
                            power = double.Parse(powerMatch.Groups["number3"].Value);

                        if (powerMatch.Groups["from"].Value.ToLower() == "ps" || powerMatch.Groups["from"].Value.ToLower() == "shp")
                            powerShp[true] = power;
                        else if (powerMatch.Groups["from"].Value.ToLower() == "ihp")
                            powerShp[false] = power;
                        else if (powerMatch.Groups["from"].Value.ToLower() == "kw")
                            powerShp[true] = power * 1.34102209;
                    }
                var speedMatch = getSpeed.Match(infobox);
                if (speedMatch.Success)
                {
                    //One of these are going to be found
                    if (!string.IsNullOrEmpty(speedMatch.Groups["number0"].Value))
                        speedKn = double.Parse(speedMatch.Groups["number0"].Value);
                    else if (!string.IsNullOrEmpty(speedMatch.Groups["number1"].Value))
                        speedKn = double.Parse(speedMatch.Groups["number1"].Value);
                    else if (!string.IsNullOrEmpty(speedMatch.Groups["number2"].Value))
                        speedKn = double.Parse(speedMatch.Groups["number2"].Value);
                    else if (!string.IsNullOrEmpty(speedMatch.Groups["number3"].Value))
                        speedKn = double.Parse(speedMatch.Groups["number3"].Value);

                    //If the regex read 24.50 as 2450, fix it
                    if (speedKn > 100)
                        speedKn /= 10.0;
                    if (speedKn > 100)
                        speedKn /= 10.0;

                }
            }
        }
    }
}