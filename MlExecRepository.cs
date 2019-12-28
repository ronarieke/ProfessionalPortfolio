using SimbaForceFrontend.Models.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SimbaForceFrontend.Models.Repository
{
    public class MlExecRepository

    {
        private readonly mlexecdbcontext context;
        public MlExecRepository(mlexecdbcontext context_)
        {
            context = context_;
        }
        public IEnumerable<TradeEntry> tradeEntries(Func<TradeEntry, bool> fxn)
        {
            return context.tradeEntry.Where(fxn);

        }
        public List<T> Get<T>(Func<dynamic, bool> fxn)
        {
            {
                switch (typeof(T).Name)
                {
                    case "TradeEntry":
                        return context.tradeEntry.Where((Func<TradeEntry, bool>)fxn).ToList() as List<T>;
                    case "InstrumentPerformance":
                        return context.instrumentPerformance.Where((Func<InstrumentPerformance, bool>)fxn).ToList() as List<T>; ;
                    case "UserRecord":
                        return context.userRecord.Where((Func<UserRecord, bool>)fxn).ToList() as List<T>; ;
                    default:
                        return new List<T>();
                }
            }
        }
        public static void fixBadDate(ref TradeEntry trdA)
        {
            DateTime FutureDate = new DateTime(2050, 1, 1);
            DateTime badDate = new DateTime(1970, 1, 1);
            if (trdA.dateClosed < badDate)
            {
                trdA.dateClosed = FutureDate;
            }
            if (trdA.dateEntered < badDate)
            {
                trdA.dateEntered = badDate;
            }
        }
        public void Set<T>(dynamic tpe)
        {
            {
                switch (typeof(T).Name)
                {
                    case "TradeEntry":
                        TradeEntry trd = tpe;
                        var tpeU = context.tradeEntry.FirstOrDefault(trdE => trdE.id == trd.id);
                        if (tpeU == null) { trd.id = Guid.NewGuid().ToString(); fixBadDate(ref trd); context.tradeEntry.Add(trd); }
                        tpeU = trd;
                        break;
                    case "Trade":
                        Trade trd_a = tpe;
                        var tpeU2 = context.trade.FirstOrDefault(trd2 => trd2.id == trd_a.id);
                        if (tpeU2 == null)
                        {
                            trd_a.guid = Guid.NewGuid().ToString();
                            context.trade.Add(trd_a);
                        }
                        tpeU2 = trd_a;
                        break;
                    case "InstrumentPerformance":
                        InstrumentPerformance trd_b = tpe;
                        var tpeU3 = context.instrumentPerformance.FirstOrDefault(trd3 => trd3.id == trd_b.id);
                        if (tpeU3 == null) { trd_b.id = Guid.NewGuid().ToString(); context.instrumentPerformance.Add(trd_b); }
                        tpeU3 = trd_b;
                        break;
                    default:
                        break;

                }
                context.SaveChanges();
            }
        }
        public void Delete<T>(dynamic tpe)
        {
            {
                switch (typeof(T).Name)
                {
                    case "TradeEntry":
                        TradeEntry tpo = tpe;
                        context.tradeEntry.Remove(context.tradeEntry.First(trdE => trdE.id == tpo.id));
                        break;
                    case "InstrumentPerformance":
                        InstrumentPerformance tpo_1 = tpe;
                        context.instrumentPerformance.Remove(context.instrumentPerformance.First(tpx => tpx.id == tpo_1.id));
                        break;
                    case "UserRecord":
                        UserRecord tpo_2 = tpe;
                        context.userRecord.Remove(context.userRecord.First(tpx => tpx.id == tpo_2.id));
                        break;
                    default:
                        break;
                }
                context.SaveChanges();
            }
        }
    }
}