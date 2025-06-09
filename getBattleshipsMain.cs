
using System;
using System.Dynamic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

/// <summary>
/// Get the battleships' displacement, speed, and horsepower from wikipedia, or from a local file with manually written to cover errors/inconsistensies on wikipedia
/// </summary>
public static class getBattleships
{

    public async static Task Main(string[] args)
    {
        Dictionary<(string, int), Ship> battleships = new();
        Dictionary<(string, int), Ship> battlecruisers = new();

        bool skipDownload = false;
        if (args.Length >= 1)
        {
            if (args[0] == "skip")
            { skipDownload = true; }
        }

        foreach (var line in File.ReadAllLines("battlecruisers.csv"))
        {
            var tokens = line.Split(',', StringSplitOptions.TrimEntries);
            //Skip the header
            if (tokens[0] == "name")
            {
                continue;
            }
            else
            {
                Ship newShip = new Ship();
                //The format must be the same as that we print as
                if (!int.TryParse(tokens[9], out int year))
                    continue;
                if (!double.TryParse(tokens[8], out double speed) || speed == 0)
                    continue;
                newShip.speedKn = speed;

                newShip.Name = tokens[0];
                newShip.ClassName = tokens[1];
                newShip.Navy = tokens[2];
                if (tokens[3] != "null" && double.TryParse(tokens[3], out double standardDisplacement))
                    newShip.displacementMT[Ship.Displacement.Standard] = standardDisplacement;

                if (tokens[4] != "null" && double.TryParse(tokens[4], out double deepDisplacement))
                    newShip.displacementMT[Ship.Displacement.Deep] = deepDisplacement;

                if (tokens[5] != "null" && double.TryParse(tokens[5], out double normalDisplacement))
                    newShip.displacementMT[Ship.Displacement.Normal] = normalDisplacement;

                if (tokens[6] != "null" && double.TryParse(tokens[6], out double indicatedHorsepower))
                    newShip.powerShp[false] = indicatedHorsepower;

                if (tokens[7] != "null" && double.TryParse(tokens[7], out double shaftHorsepower))
                    newShip.powerShp[true] = shaftHorsepower;
                battlecruisers[(tokens[0].ToLower(), year)] = newShip;
            }
        }



        if (skipDownload)
        {
            foreach (var line in File.ReadAllLines("battleship.csv"))
            {
                var tokens = line.Split(',', StringSplitOptions.TrimEntries);
                //Skip the header
                if (tokens[0] == "name")
                {
                    continue;
                }
                else
                {
                    Ship newShip = new Ship();
                    //The format must be the same as that we print as
                    if (!int.TryParse(tokens[9], out int year))
                        continue;
                    if (!double.TryParse(tokens[8], out double speed) || speed == 0)
                        continue;
                    newShip.speedKn = speed;

                    newShip.Name = tokens[0];
                    newShip.ClassName = tokens[1];
                    newShip.Navy = tokens[2];
                    if (tokens[3] != "null" && double.TryParse(tokens[3], out double standardDisplacement))
                        newShip.displacementMT[Ship.Displacement.Standard] = standardDisplacement;

                    if (tokens[4] != "null" && double.TryParse(tokens[4], out double deepDisplacement))
                        newShip.displacementMT[Ship.Displacement.Deep] = deepDisplacement;

                    if (tokens[5] != "null" && double.TryParse(tokens[5], out double normalDisplacement))
                        newShip.displacementMT[Ship.Displacement.Normal] = normalDisplacement;

                    if (tokens[6] != "null" && double.TryParse(tokens[6], out double indicatedHorsepower))
                        newShip.powerShp[false] = indicatedHorsepower;

                    if (tokens[7] != "null" && double.TryParse(tokens[7], out double shaftHorsepower))
                        newShip.powerShp[true] = shaftHorsepower;
                    battleships[(tokens[0].ToLower(), year)] = newShip;
                }
            }
        }
        else
        {


            //Semi-AI generated text to download the wikitext for the main list
            //The method for downloading the page is AI, but all the null checks and comments are human generated
            string title = "List_of_battleships";
            string url = $"https://en.wikipedia.org/w/api.php?action=query&prop=revisions&rvprop=content&format=json&titles={title}";

            using HttpClient client = new HttpClient();
            string response = await client.GetStringAsync(url);
            JObject json = JObject.Parse(response);

            //Assume the json looks like I expect, but do check for errors
            var pages = json["query"]?["pages"];
            if (pages == null)
            {
                Console.Error.WriteLine("No data received, likely internet error, or page has been moved or the structure has been modified");
                return;
            }
            //My guess is that there will only be one page, but you never know
            foreach (var page in pages)
            {
                //It is not null, I know it is not null, but the compiler doesn't
                string? wikitext = (string?)page?.First?["revisions"]?[0]?["*"];
                if (wikitext == null)
                    continue;
                //End of AI generated code

                //Use a regex to search for the ships, they are in a wikitext style list, so the lines with the ship will look something like this:
                /*This example text is included so you can try out the Regex for yourself
    | {{SMS|Árpád||2}} || 1901-09-11{{sfn|Gardiner|Chesneau|Kolesnik|1979|p=272}} ||{{sclass|Habsburg|battleship|4}}
    |[[Pre-dreadnought]]||{{navy|Austria-Hungary}}
    |Awarded to UK 1920, scrapped 1921{{sfn|Gardiner|Chesneau|Kolesnik|1979|p=272}}
    |-
    | {{HMS|Africa|1905|2}} || 1904-05-20{{sfn|Gardiner|Chesneau|Kolesnik|1979|p=38}} ||{{sclass|King Edward VII|battleship|4}}||[[Pre-dreadnought|Semi-dreadnought]]||{{navy|United Kingdom}}|| Broken up, 1920{{sfn|Gardiner|Chesneau|Kolesnik|1979|p=38}}
    |-
    | {{HMS|Agamemnon|1906|3}} || 1906-06-23{{sfn|Gardiner|Chesneau|Kolesnik|1979|p=40}} ||{{sclass|Lord Nelson|battleship|4}}
    |[[Dreadnought#All-big-gun mixed-calibre ships|Semi-dreadnought]]||{{navy|United Kingdom}}|| Broken up, 1927{{sfn|Gardiner|Chesneau|Kolesnik|1979|p=40}}
    |-
    | {{HMS|Agincourt|1913|3}} || 1913-01-22 ||
    |[[Dreadnought]]||{{navy|United Kingdom}}|| Ex-Ottoman ''Sultân Osmân-ı Evvel'', former Brazilian ''Rio de Janeiro'', seized near completion, 31 July 1914
    |-
    | {{HMS|Ajax|1912|3}} || 1912-03-21 ||{{sclass|King George V|battleship (1911)|4}} (1911)
    |[[Dreadnought#Super-dreadnoughts|Super-dreadnought]]||{{navy|United Kingdom}}||
    |-
                */

                //The table essentially contain a link to the  ship || the date in service, maybe with source || class link || class || navy operator || fate

                //I will try to explain each section of the regex string in the comments below the string 
                //You should turn off text wrap for it to be clear 
                Regex MatchShipLine = new Regex(@"(?<=\|-\n)\|.*?({{.*?}}).*?[\|\||\n].*?(\d+-\d+-\d+).*?[\|\||\n](.*?)[\|\||\n\|].*?\[\[(.*?)\]\].*?[\|\||\n].*?({{.*?}})");
                /*                                +-A------++B+C+-D------++C++-E--------++C++---F--------+C++-E--------++C++--G----++C+++E----------++C++-E----------+C++-E----------+C++-H----
                A: Positive look-behind, find anything after a newline, a |, and a space
                B: Start of line
                C: Any characters betwixt elements, matches as FEW as possible
                D: Capture group, captures the wikipedia link to the ship
                E: Table seperator or however you spell it
                F: Capture YEAR-MONTH-DAY ship was launched, often 4 chars, 2 chars, and 2 chars, but not always
                G: Captures link to ship type
                H: captures link to operating navy
                */
                var Matches = MatchShipLine.Matches(wikitext);
                foreach (Match MatchShipLineMatch in Matches)
                {
                    //Console.WriteLine("MATCHED SHIP WITH THE FOLLOWING DATA");
                    string fullMatch = MatchShipLineMatch.Groups[0].Value;
                    //      Console.WriteLine($"Full match =\"{fullMatch}\"");
                    string shipLink = MatchShipLineMatch.Groups[1].Value;
                    //This will be a template {{ship|name|disambiquation|display}}
                    //Or a navy specific like {{hms|name|pendant or year|display}}
                    //There will be 4 things to catch either way, and we don't care about the last
                    //This Regex gets all individual parts
                    Regex splitShipLink = new Regex(@"{{(.*?)\|(.*?)\|(.*?)\|(.*?)[\|(.*?)}}|}}]?");
                    var shiplinkMatches = splitShipLink.Match(shipLink);
                    if (!shiplinkMatches.Success)
                    {
                        Console.Error.WriteLine("Could not find ship name in " + shipLink + " in " + fullMatch);
                        return;
                    }
                    string shipName;

                    string altName;

                    if (shiplinkMatches.Groups[1].Value.ToLower() == "ship")
                    {
                        shipName = shiplinkMatches.Groups[2].Value + "_" + shiplinkMatches.Groups[3].Value + (shiplinkMatches.Groups[4].Value.Count() > 0 ? "_(" + shiplinkMatches.Groups[4].Value + ")" : "");
                        altName = shiplinkMatches.Groups[2].Value + "_" + shiplinkMatches.Groups[3].Value;
                    }
                    else
                    {

                        shipName = (shiplinkMatches.Groups[1].Value.ToLower() != "ship" ? shiplinkMatches.Groups[1].Value + "_" : "") + shiplinkMatches.Groups[2].Value + (shiplinkMatches.Groups[3].Value.Count() > 0 ? "_(" + shiplinkMatches.Groups[3].Value + ")" : "");
                        altName = (shiplinkMatches.Groups[1].Value.ToLower() != "ship" ? shiplinkMatches.Groups[1].Value + "_" : "") + shiplinkMatches.Groups[2].Value;
                    }

                    shipName = shipName.Replace(" ", "_");
                    altName = altName.Replace(" ", "_");
                    string launchDate = MatchShipLineMatch.Groups[2].Value;


                    //Split out the class name from this
                    string classLink = MatchShipLineMatch.Groups[3].Value;
                    //This is done with a simpler regex than the name of the ship, we have only class and type (e.g. battleship):
                    Regex splitShipClass = new Regex(@"{{sclass\|(.*?)\|(.*?)\|.*?}}");
                    var shipClassMatches = splitShipLink.Match(classLink);
                    string shipClass;
                    if (!shipClassMatches.Success)
                    {
                        //Just use the ship title if this is a one-off
                        shipClass = shipName;
                    }
                    else
                    {
                        if (shipClassMatches.Groups[3].Value.Length > 0)
                            shipClass = shipClassMatches.Groups[2].Value + "-class_" + shipClassMatches.Groups[3];
                        else
                            shipClass = shipClassMatches.Groups[2].Value;
                    }

                    //The type is either just written, or there is a |, in which case we are interested in the part to the right
                    string typeLink = MatchShipLineMatch.Groups[4].Value;
                    var types = typeLink.Split('|');
                    string type = types[types.Length - 1];
                    string navyLink = MatchShipLineMatch.Groups[5].Value;
                    //Finally split the navy from the navy link:
                    Regex splitShipnavy = new Regex(@"{{navy\|(.*?)}}");
                    var shipNavyMatches = splitShipnavy.Match(navyLink);
                    if (!shipNavyMatches.Success)
                    {
                        Console.Error.WriteLine("Could not find ship navy in " + navyLink + " in " + fullMatch);
                        return;
                    }
                    string[] navy = shipNavyMatches.Groups[1].Value.Split("|", StringSplitOptions.RemoveEmptyEntries);

                    Ship ship = new Ship(shipName, altName, launchDate, shipClass, type, navy[0]);
                    Console.WriteLine($"{ship.Name.ToLower()}-{ship.LaunchDate.Year}");
                    battleships[(ship.Name.ToLower(), ship.LaunchDate.Year)] = ship;
                }
            }

            //Try downloading from wikipedia
            int i = 0;
            foreach (var ship in battleships.Values)
            {
                Console.WriteLine($"\n\nReading {ship.Name} ({ship.LaunchDate.Year}) {++i}/{battleships.Count}");
                await ship.DownloadData();
            }

            //Now also load from the manually written file, to overwrite errors on Wikipedia
            string manualName = "battleshipCorrections.csv";
            if (args.Count() > 0)
                manualName = args[0];

            //Read all lines in the manual file
            foreach (var line in File.ReadAllLines(manualName))
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
                    if (!int.TryParse(tokens[9], out int year))
                        continue;
                    if (!double.TryParse(tokens[8], out double speed) || speed == 0)
                        continue;

                    if (!battleships.ContainsKey((tokens[0].ToLower(), year)))
                        continue;
                    battleships[(tokens[0].ToLower(), year)].speedKn = speed;

                    if (tokens[3] != "null" && double.TryParse(tokens[3], out double standardDisplacement))
                        battleships[(tokens[0].ToLower(), year)].displacementMT[Ship.Displacement.Standard] = standardDisplacement;

                    if (tokens[4] != "null" && double.TryParse(tokens[4], out double deepDisplacement))
                        battleships[(tokens[0].ToLower(), year)].displacementMT[Ship.Displacement.Deep] = deepDisplacement;

                    if (tokens[5] != "null" && double.TryParse(tokens[5], out double normalDisplacement))
                        battleships[(tokens[0].ToLower(), year)].displacementMT[Ship.Displacement.Normal] = normalDisplacement;

                    if (tokens[6] != "null" && double.TryParse(tokens[6], out double indicatedHorsepower))
                        battleships[(tokens[0].ToLower(), year)].powerShp[false] = indicatedHorsepower;

                    if (tokens[7] != "null" && double.TryParse(tokens[7], out double shaftHorsepower))
                        battleships[(tokens[0].ToLower(), year)].powerShp[true] = shaftHorsepower;

                }
            }

            //Remove still faulty ships

            var noDisplacementShips = battleships.Values.Where(s => !s.displacementMT.ContainsKey(Ship.Displacement.Standard) && !s.displacementMT.ContainsKey(Ship.Displacement.Deep) && !s.displacementMT.ContainsKey(Ship.Displacement.Normal)).ToList();
            Console.WriteLine("");
            Console.WriteLine("Removing ships where we failed to get displacement:");
            foreach (var ship in noDisplacementShips)
            {
                Console.WriteLine(ship.Name);
                battleships.Remove((ship.Name, ship.LaunchDate.Year));
            }
            var noPowerShips = battleships.Values.Where(s => !s.powerShp.ContainsKey(true) && !s.powerShp.ContainsKey(false)).ToList();
            Console.WriteLine("");
            Console.WriteLine("Removing ships where we failed to get power:");
            foreach (var ship in noPowerShips)
            {
                Console.WriteLine(ship.Name);
                battleships.Remove((ship.Name, ship.LaunchDate.Year));
            }

            var noSpeedShips = battleships.Values.Where(s => s.speedKn == 0);
            Console.WriteLine("");
            Console.WriteLine("Removing ships where we failed to get speed:");
            foreach (var ship in noSpeedShips)
            {
                Console.WriteLine(ship.Name);
                battleships.Remove((ship.Name, ship.LaunchDate.Year));
            }


            //Now output the raw data as CSV
            List<String> data = new List<String>();
            data.Add($"name,class,navy,standard displacement,deep load displacement,normal displacement,indicated horsepower,shaft horsepower,speed,year");
            foreach (Ship ship in battleships.Values)
            {
                data.Add($"{ship.Name},{ship.ClassName},{ship.Navy},{(ship.displacementMT.ContainsKey(Ship.Displacement.Standard) ? ship.displacementMT?[Ship.Displacement.Standard] : "null")},{(ship.displacementMT.ContainsKey(Ship.Displacement.Deep) ? ship.displacementMT?[Ship.Displacement.Deep] : "null")},{(ship.displacementMT.ContainsKey(Ship.Displacement.Normal) ? ship.displacementMT?[Ship.Displacement.Normal] : "null")},{(ship.powerShp.ContainsKey(false) ? ship.powerShp?[false] : "null")},{(ship.powerShp.ContainsKey(true) ? ship.powerShp?[true] : "null")},{ship.speedKn},{ship.LaunchDate.Year}");
            }
            System.IO.File.WriteAllLines("battleship.csv", data);

        }


        //Fix missing shaft horsepower, assuming 95% efficiency above 1915, otherwise 90%
        //Also get the total difference betwixt the three types of displacements for each nation, so we can convert betwixt them
        Dictionary<string, List<double>> standardToNormalRatios=new();
        Dictionary<string, List<double>> standardToDeepRatios=new();
        Dictionary<string, List<double>> normalToDeepRatios=new();
        standardToNormalRatios["generic"] = new();
        standardToDeepRatios["generic"] = new();
        foreach (Ship BB in battleships.Values)
        {
            if (!BB.powerShp.ContainsKey(false))
                BB.powerShp[true] = BB.powerShp[false] * (BB.LaunchDate.Year > 1904 ? 0.95 : 0.9);
            if (BB.displacementMT.ContainsKey(Ship.Displacement.Standard) && BB.displacementMT.ContainsKey(Ship.Displacement.Normal))
            {
                if (!standardToNormalRatios.ContainsKey(BB.Navy))
                    standardToNormalRatios[BB.Navy] = new();
                double ratio = BB.displacementMT[Ship.Displacement.Normal] / BB.displacementMT[Ship.Displacement.Standard];
                standardToNormalRatios[BB.Navy].Add(ratio);
                standardToNormalRatios["generic"].Add(ratio);
            }
            if (BB.displacementMT.ContainsKey(Ship.Displacement.Standard) && BB.displacementMT.ContainsKey(Ship.Displacement.Deep))
            {
                if (!standardToDeepRatios.ContainsKey(BB.Navy))
                    standardToDeepRatios[BB.Navy] = new();
                double ratio = BB.displacementMT[Ship.Displacement.Deep] / BB.displacementMT[Ship.Displacement.Standard];
                standardToDeepRatios[BB.Navy].Add(ratio);
                standardToDeepRatios["generic"].Add(ratio);
            }
            if (BB.displacementMT.ContainsKey(Ship.Displacement.Normal) && BB.displacementMT.ContainsKey(Ship.Displacement.Deep))
            {
                if (!normalToDeepRatios.ContainsKey(BB.Navy))
                    normalToDeepRatios[BB.Navy] = new();
                double ratio = BB.displacementMT[Ship.Displacement.Deep] / BB.displacementMT[Ship.Displacement.Normal];
                normalToDeepRatios[BB.Navy].Add(ratio);
                normalToDeepRatios["generic"].Add(ratio);
            }

        }
        foreach (Ship BC in battlecruisers.Values)
        {
            if (!BC.powerShp.ContainsKey(false))
                BC.powerShp[true] = BC.powerShp[false] * (BC.LaunchDate.Year > 1904 ? 0.95 : 0.9);
        }
        
    }
}