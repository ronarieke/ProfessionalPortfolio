using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FXFincApp.Functionality
{


    public class Candle
    {
        public bool complete { get; set; }
        public Mid mid { get; set; }
        public string time { get; set; }
        public int volume { get; set; }
    }
    public class Mid
    {
        public string c { get; set; }
        public string h { get; set; }
        public string l { get; set; }
        public string o { get; set; }
    }
    public class Instrument
    {
        public string displayName { get; set; }
        public int displayPrecision { get; set; }
        public string marginRate { get; set; }
        public string maximumOrderUnits { get; set; }
        public string maximumPositionSize { get; set; }
        public string maximumTrailingStopDistance { get; set; }
        public string minimumTradeSize { get; set; }
        public string minimumTrailingStopDistance { get; set; }
        public string name { get; set; }
        public int pipLocation { get; set; }
        public int tradeUnitsPrecision { get; set; }
        public string type { get; set; }
    }
    public static class Requestor
    {
        public static string url = "https://api-fxtrade.oanda.com"; // "https://api-fxpractice.oanda.com";
        static string granularity = "M1";
        public static int count = 61;
        public static int forwardCount = 15;
        static Tuple<string, string, string> access = Tuple.Create<string, string, string>(key, account, url);
        static string auth { get { return String.Format("Bearer {0}", access.Item1); } }
        static RestClient client = new RestClient(access.Item3);
        public static void AddHeaders(ref RestRequest request)
        {
            request.AddHeader("Authorization", auth);
            request.AddHeader("Content-Type", "application/json");
        }
        static KeyValuePair<string, string> kvp(string a, string b)
        {
            return new KeyValuePair<string, string>(a, b);
        }
        static void AddParameters(ref RestRequest request, List<KeyValuePair<string, string>> parameters)
        {
            foreach (var kvp in parameters)
                request.AddQueryParameter(kvp.Key, kvp.Value);
        }
        public static List<Instrument> instruments()
        {
            RestRequest request = new RestRequest(String.Format("/v3/accounts/{0}/instruments", access.Item2), Method.GET);
            AddHeaders(ref request);
            List<Instrument> instrs = JsonConvert.DeserializeObject<List<Instrument>>(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<dynamic>(client.Get(request).Content).instruments));
            instrs = instrs.Where(instr => new List<string> { "HUF", "ZAR", "DKK", "SEK", "MXN", "TRY", "SGD", "CNH", "PLN", "HKD" }.Any(c => instr.name.Contains(c)).Equals(false)).ToList();
            return instrs.OrderBy(instr => instr.name).ToList();
        }
        static string pad(int x)
        {
            if (x < 10) { return "0" + x.ToString(); }
            return x.ToString();
        }
        public static string deFormatDate(DateTime date)
        {
            return String.Format("{0}-{1}-{2}T{3}:{4}:{5}.000000000Z", date.Year, pad(date.Month), pad(date.Day), pad(date.Hour), pad(date.Minute), pad(date.Second));
        }
        public static DateTime formatDate(string dte)
        {
            DateTime date = DateTime.Now;
            if (DateTime.TryParse(dte, out date))
            {
                return date;
            }
            else
            {
                Console.WriteLine("date isn't parsing: {0}", dte);
                return date;
            }
        }
        public static List<Candle> candles(string instrument, string granularity, int count, DateTime start)
        {
            RestRequest request = new RestRequest(String.Format("/v3/instruments/{0}/candles", instrument), Method.GET);
            AddHeaders(ref request);
            AddParameters(ref request, new List<KeyValuePair<string, string>> { kvp("granularity", granularity), kvp("count", (count).ToString()), kvp("price", "M") });
            return JsonConvert.DeserializeObject<List<Candle>>(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<dynamic>(client.Get(request).Content).candles));
        }
        static double d(string a) { return Convert.ToDouble(a); }
        public static List<Tuple<string, List<Tuple<DateTime, Tuple<double, double, double, double, double>>>>> retrieveDataFor(DateTime date, List<Instrument> instruments, string granularity = "M1", int ct = 0)
        {
            return instruments.Select(instr => Tuple.Create<string, List<Tuple<DateTime, Tuple<double, double, double, double, double>>>>(instr.name, candles(instr.name, granularity, ct == 0 ? count : ct, date).Select(cndle => Tuple.Create<DateTime, Tuple<double, double, double, double, double>>(formatDate(cndle.time), Tuple.Create<double, double, double, double, double>(d(cndle.mid.o), d(cndle.mid.h), d(cndle.mid.l), d(cndle.mid.c), cndle.volume))).ToList())).ToList();

        }
        public static double[,] numpyArray(List<Tuple<string, List<Tuple<DateTime, Tuple<double, double, double, double, double>>>>> data)
        {
            double[,] numpy = new double[data[0].Item2.Count, 5 * data.Count()];
            for (int x = 0; x < data[0].Item2.Count; x++)
            {
                for (int y = 0; y < data.Count(); y++)
                {
                    int i = y * 5;
                    numpy[x, i] = data[y].Item2[x].Item2.Item1;
                    numpy[x, i + 1] = data[y].Item2[x].Item2.Item2;
                    numpy[x, i + 2] = data[y].Item2[x].Item2.Item3;
                    numpy[x, i + 3] = data[y].Item2[x].Item2.Item4;
                    numpy[x, i + 4] = data[y].Item2[x].Item2.Item5;
                }
            }
            return numpy;
        }

        static DateTime start
        {
            get
            {
                DateTime dte = new DateTime(2013, 5, 28);
                while (!dte.DayOfWeek.Equals(DayOfWeek.Sunday)) { dte = dte.AddDays(1); }
                return new DateTime(dte.Year, dte.Month, dte.Day, 16, 0, 0);
            }
        }
        static string filizeDate(DateTime date)
        {
            return String.Format("{0}_{1}_{2}_{3}.json", date.Year, pad(date.Month), pad(date.Day), pad(date.Hour));
        }
        public static void collectADay(ref DateTime date, List<Instrument> instruments)
        {
            File.WriteAllText(String.Format(@"C:/users/v-roriek/repository/{0}", filizeDate(date)), JsonConvert.SerializeObject(numpyArray(retrieveDataFor(date, instruments))));
        }
        public static void init()
        {
            List<Instrument> instrs = instruments();

            DateTime date = start;
            int count = 0;
            while (count < (DateTime.Now - start).TotalDays)
            {
                DateTime interim = date.AddDays(count);
                if (new DayOfWeek[] { DayOfWeek.Friday, DayOfWeek.Saturday }.Contains(interim.DayOfWeek))
                    count++;
                interim = date.AddDays(count);
                if (new DayOfWeek[] { DayOfWeek.Friday, DayOfWeek.Saturday }.Contains(interim.DayOfWeek))
                    count++;
                interim = date.AddDays(count);
                collectADay(ref interim, instrs);
                count++;
            }
        }
    }

}
