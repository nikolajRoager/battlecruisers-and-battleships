﻿
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

        foreach (var line in File.ReadAllLines("Battlecruisers.csv"))
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

                newShip.LaunchDate = new DateOnly(year,1,1);
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
                    newShip.LaunchDate = new DateOnly(year,1,1);
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
            //Await each individually, rather than in batches, to not overtax my internet
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
                //Skip empty pages, but my best guess is this never happens
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
                
                var Matches = MatchShipLine.Matches(wikitext);
                foreach (Match MatchShipLineMatch in Matches)
                {
                    string fullMatch = MatchShipLineMatch.Groups[0].Value;
                    
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
                //Wait for each download to finish before starting the next,
                //to avoid killing my internet connection
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
            List<String> _data = new List<String>();
            _data.Add($"name,class,navy,standard displacement,deep load displacement,normal displacement,indicated horsepower,shaft horsepower,speed,year");
            foreach (Ship ship in battleships.Values)
            {
                _data.Add($"{ship.Name},{ship.ClassName},{ship.Navy},{(ship.displacementMT.ContainsKey(Ship.Displacement.Standard) ? ship.displacementMT?[Ship.Displacement.Standard] : "null")},{(ship.displacementMT.ContainsKey(Ship.Displacement.Deep) ? ship.displacementMT?[Ship.Displacement.Deep] : "null")},{(ship.displacementMT.ContainsKey(Ship.Displacement.Normal) ? ship.displacementMT?[Ship.Displacement.Normal] : "null")},{(ship.powerShp.ContainsKey(false) ? ship.powerShp?[false] : "null")},{(ship.powerShp.ContainsKey(true) ? ship.powerShp?[true] : "null")},{ship.speedKn},{ship.LaunchDate.Year}");
            }
            System.IO.File.WriteAllLines("battleship.csv", _data);

        }


        //Fix missing shaft horsepower, assuming 95% efficiency above 1904, otherwise 90%
        //Also get the total difference betwixt the three types of displacements for each nation, so we can convert betwixt them
        Dictionary<string, List<double>> standardToNormalRatios = new();
        Dictionary<string, List<double>> standardToDeepRatios = new();
        Dictionary<string, List<double>> normalToDeepRatios = new();
        standardToNormalRatios["generic"] = new();
        standardToDeepRatios["generic"] = new();
        normalToDeepRatios["generic"] = new();
        foreach (Ship BB in battleships.Values)
        {
            if (!BB.powerShp.ContainsKey(true) || BB.powerShp[true]==0)
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
                if (BB.Navy == "United States")
                    Console.WriteLine("Add "+ratio+" from "+BB.Name);
                normalToDeepRatios["generic"].Add(ratio);
            }

        }
        foreach (Ship BC in battlecruisers.Values)
        {
            if (!BC.powerShp.ContainsKey(true) || BC.powerShp[true]==0)
            {
                if (BC.powerShp.ContainsKey(false))
                    BC.powerShp[true] = BC.powerShp[false] * (BC.LaunchDate.Year > 1904 ? 0.95 : 0.9);
                if (BC.displacementMT.ContainsKey(Ship.Displacement.Standard) && BC.displacementMT.ContainsKey(Ship.Displacement.Normal))
                {
                    if (!standardToNormalRatios.ContainsKey(BC.Navy))
                        standardToNormalRatios[BC.Navy] = new();
                    double ratio = BC.displacementMT[Ship.Displacement.Normal] / BC.displacementMT[Ship.Displacement.Standard];
                    standardToNormalRatios[BC.Navy].Add(ratio);
                    standardToNormalRatios["generic"].Add(ratio);
                }
                if (BC.displacementMT.ContainsKey(Ship.Displacement.Standard) && BC.displacementMT.ContainsKey(Ship.Displacement.Deep))
                {
                    if (!standardToDeepRatios.ContainsKey(BC.Navy))
                        standardToDeepRatios[BC.Navy] = new();
                    double ratio = BC.displacementMT[Ship.Displacement.Deep] / BC.displacementMT[Ship.Displacement.Standard];
                    standardToDeepRatios[BC.Navy].Add(ratio);
                    standardToDeepRatios["generic"].Add(ratio);
                }
                if (BC.displacementMT.ContainsKey(Ship.Displacement.Normal) && BC.displacementMT.ContainsKey(Ship.Displacement.Deep))
                {
                    if (!normalToDeepRatios.ContainsKey(BC.Navy))
                        normalToDeepRatios[BC.Navy] = new();
                    double ratio = BC.displacementMT[Ship.Displacement.Deep] / BC.displacementMT[Ship.Displacement.Normal];
                    normalToDeepRatios[BC.Navy].Add(ratio);
                    normalToDeepRatios["generic"].Add(ratio);
                if (BC.Navy == "United States")
                    Console.WriteLine("Add "+ratio+" from "+BC.Name);
                }
            }
        }

        Dictionary<string, double> standardToNormalRatio = new();
        Dictionary<string, double> standardToDeepRatio = new();
        Dictionary<string, double> normalToDeepRatio = new();


        //Calculate mean ratio of displacement conversion for each navy
        foreach (var navy in normalToDeepRatios.Keys)
        {
            double avg = 0;
            double var = 0;
            double sig = 0;
            if (normalToDeepRatios[navy].Count > 0)
            {
                foreach (double ratio in normalToDeepRatios[navy])
                {
                    avg += ratio / (double)normalToDeepRatios[navy].Count;
                }
                foreach (double ratio in normalToDeepRatios[navy])
                {
                    var += (ratio - avg) * (ratio - avg) / (double)normalToDeepRatios[navy].Count;
                }
                sig = Math.Sqrt(var);

                normalToDeepRatio[navy] = avg;
                Console.WriteLine(navy + " normal displacement to deep displacement " + avg + "+-" + sig);
            }
        }

        Console.WriteLine("\n\n\n");

        //Calculate mean ratio of displacement conversion for each navy
        foreach (var navy in standardToDeepRatios.Keys)
        {
            double avg = 0;
            double var = 0;
            double sig = 0;
            if (standardToDeepRatios[navy].Count > 0)
            {
                foreach (double ratio in standardToDeepRatios[navy])
                {
                    avg += ratio / (double)standardToDeepRatios[navy].Count;
                }
                foreach (double ratio in standardToDeepRatios[navy])
                {
                    var += (ratio - avg) * (ratio - avg) / (double)standardToDeepRatios[navy].Count;
                }
                sig = Math.Sqrt(var);

                Console.WriteLine(navy + " standard displacement to deep displacement " + avg + "+-" + sig);
                standardToDeepRatio[navy] = avg;
            }
        }

        Console.WriteLine("\n\n\n");
        //Calculate mean ratio of displacement conversion for each navy
        foreach (var navy in standardToNormalRatios.Keys)
        {
            double avg = 0;
            double var = 0;
            double sig = 0;
            if (standardToNormalRatios[navy].Count > 0)
            {
                foreach (double ratio in standardToNormalRatios[navy])
                {
                    avg += ratio / (double)standardToNormalRatios[navy].Count;
                }
                foreach (double ratio in standardToNormalRatios[navy])
                {
                    var += (ratio - avg) * (ratio - avg) / (double)standardToNormalRatios[navy].Count;
                }
                sig = Math.Sqrt(var);

                Console.WriteLine(navy + " standard to normal displacement " + avg + "+-" + sig);
                standardToNormalRatio[navy] = avg;
            }
        }
        

        //Now output the entire data as another CSV
        List<String> data = new List<String>();
        data.Add($"name,class,navy,normal displacement,shaft horsepower,speed,year,power to weight,type");
        foreach (Ship ship in battleships.Values)
        {
            double standardDisplacement=0;
            double deepDisplacement=0;
            double normalDisplacement=0;

            //Try to get standard or deep displacement, and estimate normal displacements from each
            if (ship.displacementMT.ContainsKey(Ship.Displacement.Standard))
            {
                standardDisplacement = ship.displacementMT[Ship.Displacement.Standard];
                if (standardToNormalRatio.ContainsKey(ship.Navy))
                    normalDisplacement = standardToNormalRatio[ship.Navy] * standardDisplacement;
                else
                    normalDisplacement=standardToNormalRatio["generic"]*standardDisplacement;
            }
            if (ship.displacementMT.ContainsKey(Ship.Displacement.Deep))
            {
                deepDisplacement = ship.displacementMT[Ship.Displacement.Deep];
                if (normalDisplacement == 0)
                {
                    if (normalToDeepRatio.ContainsKey(ship.Navy))
                            normalDisplacement = (1.0/normalToDeepRatio[ship.Navy]) * deepDisplacement;
                        else
                            normalDisplacement = (1.0/normalToDeepRatio["generic"]) * deepDisplacement;

                }
            }
            if (ship.displacementMT.ContainsKey(Ship.Displacement.Normal))
            {
                normalDisplacement= ship.displacementMT[Ship.Displacement.Normal];
            }

            data.Add($"{ship.Name},{ship.ClassName},{ship.Navy},{normalDisplacement},{ship.powerShp[true]},{ship.speedKn},{ship.LaunchDate.Year},{ship.powerShp[true]/normalDisplacement},BB");
        }
        foreach (Ship ship in battlecruisers.Values)
        {
            double standardDisplacement=0;
            double deepDisplacement=0;
            double normalDisplacement=0;

            //Try to get standard or deep displacement, and estimate normal displacements from each
            if (ship.displacementMT.ContainsKey(Ship.Displacement.Standard))
            {
                standardDisplacement = ship.displacementMT[Ship.Displacement.Standard];
                if (standardToNormalRatio.ContainsKey(ship.Navy))
                    normalDisplacement = standardToNormalRatio[ship.Navy] * standardDisplacement;
                else
                    normalDisplacement=standardToNormalRatio["generic"]*standardDisplacement;
            }
            if (ship.displacementMT.ContainsKey(Ship.Displacement.Deep))
            {
                deepDisplacement = ship.displacementMT[Ship.Displacement.Deep];
                if (normalDisplacement == 0)
                {
                    if (normalToDeepRatio.ContainsKey(ship.Navy))
                            normalDisplacement = (1.0/normalToDeepRatio[ship.Navy]) * deepDisplacement;
                        else
                            normalDisplacement = (1.0/normalToDeepRatio["generic"]) * deepDisplacement;

                }
            }
            if (ship.displacementMT.ContainsKey(Ship.Displacement.Normal))
            {
                normalDisplacement= ship.displacementMT[Ship.Displacement.Normal];
            }

            data.Add($"{ship.Name},{ship.ClassName},{ship.Navy},{normalDisplacement},{ship.powerShp[true]},{ship.speedKn},{ship.LaunchDate.Year},{ship.powerShp[true]/normalDisplacement},BC");
        }
        System.IO.File.WriteAllLines("ships_normal_power.csv", data);
        
    }
}