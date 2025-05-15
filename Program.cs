
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class Program
{
    public async static Task Main()
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
            Console.WriteLine("Analyzing page for shiplist");

            //It is not null, I know it is not null
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
            Regex MatchShipLine = new Regex(@"(?<=\|-\n)\|.*?({{.*?}}).*?[\|\||\n].*?(\d+-\d+-\d+).*?[\|\||\n].*?({{.*?}}).*?[\|\||\n\|].*?\[\[(.*?)\]\].*?[\|\||\n].*?({{.*?}})");
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
                Console.WriteLine($"Full match =\"{fullMatch}\"");
                string shipLink = MatchShipLineMatch.Groups[1].Value;
                //This will be a template {{ship|name|disambiquation|display}}
                //Or a navy specific like {{hms|name|pendant or year|display}}
                //There will be 4 things to catch either way, and we don't care about the last
                //This Regex gets all individual parts
                Regex splitShipLink = new Regex(@"{{(.*?)\|(.*?)\|(.*?)\|(.*?)}}");
                var shiplinkMatches=splitShipLink.Match(shipLink);
                if (!shiplinkMatches.Success)
                {
                    Console.Error.WriteLine("Could not find ship name in "+shipLink+" in "+fullMatch);
                    return;
                }
                string shipTitle;
                shipTitle= (shiplinkMatches.Groups[1].Value.ToLower()!="ship"? shiplinkMatches.Groups[1].Value+"_":"")+shiplinkMatches.Groups[2].Value+(shiplinkMatches.Groups[3].Value.Count()>0? "_("+shiplinkMatches.Groups[3].Value+")":"");
                shipTitle=shipTitle.Replace(" ", "_");
                Console.Write($"Ship =\"{shipTitle}\"");
                string launched = MatchShipLineMatch.Groups[2].Value;

                //Just get the launch date as a string
                Console.Write($"Launched   =\"{launched}\"");

                //Split out the class name from this
                string classLink = MatchShipLineMatch.Groups[3].Value;
                //This is done with a simpler regex than the name of the ship, we have only class and type (e.g. battleship):
                Regex splitShipClass = new Regex(@"{{sclass\|(.*?)\|(.*?)\|(.*?)}}");
                var shipClassMatches=splitShipLink.Match(classLink);
                string sclass;
                if (!shipClassMatches.Success)
                {
                    sclass = shipTitle;
                }
                else
                {
                    sclass = shipClassMatches.Groups[2].Value;
                }
                Console.Write($"Class =\"{sclass}\"");

                //The type is either just written, or there is a |, in which case we are interested in the part to the right
                string typeLink = MatchShipLineMatch.Groups[4].Value;
                var types = typeLink.Split('|');
                string type = types[types.Length - 1];
                Console.Write($"Type =\"{type}\"");
                string navyLink= MatchShipLineMatch.Groups[5].Value;
                //Finally split the navy from the navy link:
                Regex splitShipnavy  = new Regex(@"{{navy\|(.*?)[\|.+]?}}");
                var shipNavyMatches=splitShipnavy.Match(navyLink);
                if (!shipNavyMatches.Success)
                {
                    Console.Error.WriteLine("Could not find ship navy in "+navyLink+" in "+fullMatch);
                    return;
                }
                string navy = shipNavyMatches.Groups[1].Value;

                Console.WriteLine($"Navy =\"{navy}\"");
            }

        }

    }
}