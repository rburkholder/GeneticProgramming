using System;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

//using QDCustom;
//using OneUnified.IB;
using OneUnified.SmartQuant;
using OneUnified.GeneticProgramming;

using SmartQuant.FIX;
using SmartQuant.Data;
using SmartQuant.Series;
using SmartQuant.Trading;
using SmartQuant.Charting;
using SmartQuant.Execution;
using SmartQuant.Providers;
using SmartQuant.Indicators;
using SmartQuant.Instruments;
//using SmartQuant.Optimization;

//namespace UI {

  [StrategyComponent("{1bffce05-3830-484b-aabc-21297ab3fc2a}", ComponentType.ATSComponent, Name = "atscQT", Description = "")]
  public class atscQT : ATSComponent {
    
    ATSStrategy strat;
    ATSMetaStrategy ms;
    MarketManager mm;

    SingleOrder marketOrder;
    int OrderSize = 100;

    DateTime dtCurrentDay;
    //DailySeries daily;

    DoubleSeries dsZero;

    //QTStrategy gpstrategy;

    bool ExitSignal;
    bool BuySignal;
    bool SellSignal;

    Individual individual;

    double dblStop;

    TimeSpan tsStart;
    TimeSpan tsStop;

    // optimization values
    int intBidAskSpreadSmoothing = 50;
    int intBidAskSmoothing = 6;
    int intTradeSmoothing = 4;
    int intER0 = 16;
    int intER1 = 50;
    const int intReturnsPeriod = 5;
    const int cntKurtosis = 20;
    const int cntSkew = 8;

    // used by Optimizer in calculations
    const int KamaFastInterval = 2;
    const int KamaSlowInterval = 30;

    const double dblKamaFastest = 2.0 / (KamaFastInterval + 1);
    const double dblKamaSlowest = 2.0 / (KamaSlowInterval + 1);

    public BarSeries bars;
    public DoubleSeries dsTrade;
    public SMA smaTrades;
    public DoubleSeries dsTradeVolume;
    public DoubleSeries dsBid;
    public DoubleSeries dsAsk;

    public DoubleSeries dsBidAskSpread;
    public SMA smaBidAskSpread;
    public DoubleSeries dsBidAskMidPoint;
    public SMA smaBidAskMidPoint;

    public DoubleSeries dsBidAskKama0;
    public DoubleSeries dsBidAskKama1;

    public DoubleSeries dsTradeER0; // Kaufman's efficiency ratio, page 732
    public DoubleSeries dsTradeER1;

    //public DoubleSeries dsBidAskER0;
    //public DoubleSeries dsBidAskER1;

    public DoubleSeries dsReturns;

    Statistics stats;
    public DoubleSeries dsKurtosis;
    public DoubleSeries dsSkew;

    public double Default = 0.0;
    

    public int ER0 {
      get { return intER0; }
      set { intER0 = value; }
    }

    public int ER1 {
      get { return intER1; }
      set { intER1 = value; }
    }

    public int BidAskSmoothing {
      get { return intBidAskSmoothing; }
      set { intBidAskSmoothing = value; }
    }

    public atscQT()
      : base() {

      //gpstrategy = new QTStrategy();

      PriceNode pn = new PriceNode();
      pn = null;
      AgoNode an = new AgoNode();
      an = null;
      VolumeNode vn = new VolumeNode();
      vn = null;



    }

    public override void Init() {

      //Console.WriteLine("We are in atscQT init.");

      //if (null == instrument) return;

      strat = base.Strategy;
      ms = strat.ATSMetaStrategy;
      mm = strat.MarketManager;

      dsZero = new DoubleSeries("Zero");
      dsZero.Color = Color.Black;

      //individual = (Individual) strat.Global[ "Individual" ];
      GPOptimizer gpo = (GPOptimizer) ms.Optimizer;
      individual = gpo.curIndividual;
      
      
      //gpstrategy.EmitSignalInit(this);
      bars = new BarSeries("Bars");

      dsTrade = new DoubleSeries("Trades");
      smaTrades = new SMA(dsTrade, intTradeSmoothing);
      dsTradeVolume = new DoubleSeries("Trade Volume");
      dsBid = new DoubleSeries("Bid");
      dsAsk = new DoubleSeries("Ask");
      dsBidAskSpread = new DoubleSeries("Bid Ask Spread");
      smaBidAskSpread = new SMA(dsBidAskSpread, intBidAskSpreadSmoothing);
      dsBidAskMidPoint = new DoubleSeries("Bid Ask Midpoint");
      smaBidAskMidPoint = new SMA(dsBidAskMidPoint, intBidAskSmoothing);

      dsBidAskKama0 = new DoubleSeries("Bid Ask Kama 0");
      dsBidAskKama1 = new DoubleSeries("Bid Ask Kama 1");

      dsTradeER0 = new DoubleSeries("Trade ER 0");
      dsTradeER1 = new DoubleSeries("Trade ER 1");

      //dsBidAskER0 = new DoubleSeries("Bid Ask ER 0");
      //dsBidAskER1 = new DoubleSeries("Bid Ask ER 1");

      dsReturns = new DoubleSeries("Trade Returns");

      stats = new Statistics(dsTrade);
      dsKurtosis = new DoubleSeries("Kurtosis");
      dsSkew = new DoubleSeries("Skew");




      /*
      dtCurrentDay = new DateTime(0);
      try {
        daily = DataManager.GetDailySeries(this.instrument);
      }
      catch {
        new Exception("couldn't load ds for instrument " + instrument.Symbol);
      }
       */

      DateTime dtStartingDate;
      if (MetaStrategyMode.Live == strat.ATSMetaStrategy.MetaStrategyMode) {
        dtStartingDate = DateTime.Today;
        string prov = "IQFeed";
        IBarFactory factory = ProviderManager.MarketDataProviders[prov].BarFactory;
        factory.Items.Clear();
        factory.Items.Add(new BarFactoryItem(BarType.Time, 60, true));

        instrument.RequestMarketData(
          ProviderManager.MarketDataProviders[prov], MarketDataType.Quote);
        instrument.RequestMarketData(
          ProviderManager.MarketDataProviders[prov], MarketDataType.Trade);

        tsStart = new TimeSpan(10, 30, 0);
        tsStop  = new TimeSpan(16, 55, 0);
      }
      else {
        dtStartingDate = strat.MetaStrategyBase.SimulationManager.EntryDate;
        IBarFactory factory = ProviderManager.MarketDataProviders[1].BarFactory;
        factory.Items.Clear();
        factory.Items.Add(new BarFactoryItem(BarType.Time, 60, true));

        tsStart = new TimeSpan( 9, 30, 0);
        tsStop  = new TimeSpan(15, 55, 0);

      }

      //Console.WriteLine( "StartingDate {0}", dtStartingDate );
      dtCurrentDay = dtStartingDate.Date;

      /*
      Bar barDay1;

      int i = daily.GetIndex(dtStartingDate.Date, EIndexOption.Prev);
      barDay1 = daily[i];
      if (barDay1.DateTime.Date == dtStartingDate.Date) {
        i--; // realtime will come up with different one from simulation, depending upon what is dateset 
        // ie, there is no bar or information for today, only the previous trading day
        barDay1 = daily[i];
      }
      */

    }

    public override void OnBar( Bar bar ) {
      //gpstrategy.EmitSignalBar(this, bar);
      bars.Add(bar);

    }

    private void SetStop( double Stop ) {
      dblStop = Stop;
    }

    public override void OnTrade( Trade trade ) {

      //Console.WriteLine( "{0} {1} {2}", trade.DateTime, trade.Price, trade.Size );
      //gpstrategy.EmitSignalTrade(this, trade);
      if (0.0 == Default) Default = trade.Price;

      dsTrade.Add(trade.DateTime, trade.Price);
      dsTradeVolume.Add(trade.DateTime, trade.Size);

      //Console.WriteLine( "rp {0} {1}", dsTrade.Count, dsReturns.Count );
      //if ( ReturnsPeriod < smaTrade.Count ) {
      //	dsReturns.Add( trade.DateTime, ( smaTrade.Last - smaTrade.Ago( ReturnsPeriod ) ) / smaTrade.Ago( ReturnsPeriod ) );
      //}
      if (intReturnsPeriod < dsTrade.Count) {
        //Console.WriteLine( "adding return" );
        dsReturns.Add(trade.DateTime, (trade.Price - dsTrade.Ago(intReturnsPeriod)) / dsTrade.Ago(intReturnsPeriod));
      }

      //Console.WriteLine( "er" );
      if (dsTrade.Count >= 3) {
        int interval = Math.Min(dsTrade.Count, intER0);
        double last = dsTrade.Last;
        double first = dsTrade.Ago(1);
        double sum = Math.Abs(last - first);
        for (int i = 2; i < interval; i++) {
          first = dsTrade.Ago(i);
          sum += Math.Abs(dsTrade.Ago(i - 1) - first);
        }

        if (0.0 == sum) {
          dsTradeER0.Add(trade.DateTime, 1);
        }
        else {
          double t = Math.Abs(last - first) / sum;
          dsTradeER0.Add(trade.DateTime, t);
        }
      }

      //Console.WriteLine( "er" );
      if (dsTrade.Count >= 3) {
        int interval = Math.Min(dsTrade.Count, intER1);
        double last = dsTrade.Last;
        double first = dsTrade.Ago(1);
        double sum = Math.Abs(last - first);
        for (int i = 2; i < interval; i++) {
          first = dsTrade.Ago(i);
          sum += Math.Abs(dsTrade.Ago(i - 1) - first);
        }

        if (0.0 == sum) {
          dsTradeER1.Add(trade.DateTime, 1);
        }
        else {
          double t = Math.Abs(last - first) / sum;
          dsTradeER1.Add(trade.DateTime, t);
        }
      }


      if (Math.Max(cntKurtosis, cntSkew) <= dsTrade.Count) {
        dsKurtosis.Add(trade.DateTime, stats.Kurtosis(cntKurtosis));
        dsSkew.Add(trade.DateTime, stats.Skewness(cntSkew));
      }




      dsZero.Add(trade.DateTime, 0);
    }

    public override void OnQuote( Quote quote ) {

      //Console.WriteLine( "onQuote {0} b/a {1}/{2}", 
      //	instrument.Symbol, quote.Bid, quote.Ask );

      if ((quote.DateTime.TimeOfDay > tsStop)
       || (quote.DateTime.TimeOfDay < tsStart)) {
        ExitSignal = true;
      }

      //gpstrategy.EmitSignalQuote(this, quote);
      dsAsk.Add(quote.DateTime, quote.Ask);
      dsBid.Add(quote.DateTime, quote.Bid);
      double t = (quote.Ask + quote.Bid) / 2.0;
      dsBidAskMidPoint.Add(quote.DateTime, t);
      if (0.0 == Default) Default = t;

      //dsBidAskRange.Add( quote.DateTime, quote.Ask - quote.Bid );
      if (quote.Ask > quote.Bid) {
        //double d = Math.Log( Math.Abs( Math.Log( quote.Ask ) - Math.Log( quote.Bid ) ) );
        //double d = Math.Log( Math.Log( quote.Ask ) - Math.Log( quote.Bid ) );
        //double d = Math.Abs( Math.Log( quote.Ask ) - Math.Log( quote.Bid ) );
        double d = quote.Ask - quote.Bid;
        //Console.WriteLine( "{0}", d );
        dsBidAskSpread.Add(quote.DateTime, d);
      }


      //
      // calculate KAMA
      //

      if (0 == dsBidAskKama0.Count) {
        dsBidAskKama0.Add(quote.DateTime, dsBidAskMidPoint.Last);
      }
      else {
        if (0 == dsTradeER0.Count) {
          dsBidAskKama0.Add(quote.DateTime, dsBidAskMidPoint.Last);
        }
        else {
          double er = dsTradeER0.Last;
          double sc = Math.Pow(er * (dblKamaFastest - dblKamaSlowest) + dblKamaSlowest, 2);
          dsBidAskKama0.Add(quote.DateTime, dsBidAskKama0.Last + sc * (dsBidAskMidPoint.Last - dsBidAskKama0.Last));
        }
      }

      if (0 == dsBidAskKama1.Count) {
        dsBidAskKama1.Add(quote.DateTime, dsBidAskMidPoint.Last);
      }
      else {
        if (0 == dsTradeER1.Count) {
          dsBidAskKama1.Add(quote.DateTime, dsBidAskMidPoint.Last);
        }
        else {
          double er = dsTradeER1.Last;
          double sc = Math.Pow(er * (dblKamaFastest - dblKamaSlowest) + dblKamaSlowest, 2);
          dsBidAskKama1.Add(quote.DateTime, dsBidAskKama1.Last + sc * (dsBidAskMidPoint.Last - dsBidAskKama1.Last));
        }
      }


      //BuySignal = individual.LongSignal.EvaluateBool(this);
      //SellSignal = individual.ShortSignal.EvaluateBool(this);



      if (ExitSignal) {
        if (HasPosition) {
          switch (Position.Side) {
            case PositionSide.Long:
              OnNewLongExit(Instrument, (int)Math.Abs(Position.Qty), "Long Stop");
              break;
            case PositionSide.Short:
              OnNewShortExit(Instrument, (int)Math.Abs(Position.Qty), "Short Stop");
              break;
          }

        }
      }
      else {
        if (BuySignal ^ SellSignal) { // one or the other but not both
          if (HasPosition) {
            switch (Position.Side) {
              case PositionSide.Long:
                if (SellSignal) {
                  OnNewLongExit(Instrument, (int)Math.Abs(Position.Qty), "Long Exit");
                  OnNewShortEntry(Instrument, OrderSize, "Short Reversal");
                }
                break;
              case PositionSide.Short:
                if (BuySignal) {
                  OnNewShortExit(Instrument, (int)Math.Abs(Position.Qty), "Short Exit");
                  OnNewLongEntry(Instrument, OrderSize, "Long Reversal");
                }
                break;
            }
          }
          else {
            if (BuySignal) {
              OnNewLongEntry(Instrument, OrderSize, "Long Entry");

            }
            if (SellSignal) {
              OnNewShortEntry(Instrument, OrderSize, "Short Entry");
            }
          }
        }
      }
    }

    public void OnNewLongEntry( Instrument instrument, int Size, string sComment ) {
      marketOrder = MarketOrder(instrument, Side.Buy, Size);
      //marketOrder = MarketOrder( Side.Buy, Size );
      marketOrder.Text = sComment;
      marketOrder.Send();
      //		Console.WriteLine( instrument.Symbol + " " + sComment );
    }

    public void OnNewShortEntry( Instrument instrument, int Size, string sComment ) {
      marketOrder = MarketOrder(instrument, Side.SellShort, Size);
      //marketOrder = MarketOrder( Side.SellShort, intSize );
      marketOrder.Text = sComment;
      marketOrder.Send();
      //		Console.WriteLine( instrument.Symbol + " " + sComment );
    }

    public void OnNewLongExit( Instrument instrument, int Size, string sComment ) {
      marketOrder = MarketOrder(instrument, Side.Sell, Size);
      //marketOrder = MarketOrder( Side.Sell, intSize );
      marketOrder.Text = sComment;
      marketOrder.Send();
      //		Console.WriteLine(instrument.Symbol + " " + sComment );
    }

    public void OnNewShortExit( Instrument instrument, int Size, string sComment ) {
      marketOrder = MarketOrder(instrument, Side.Buy, Size);
      //marketOrder = MarketOrder( Side.Buy, intSize );
      marketOrder.Text = sComment;
      marketOrder.Send();
      //		Console.WriteLine( instrument.Symbol + " " + sComment );
    }

    public override void OnPositionOpened() {

      /*
      Console.WriteLine( "{0} {1} Position Opened {2:#.00} {3:#.00} {4:#.00} {5:#.00} {6:#.00} {7:#.00}",
        Position.Transactions.Last.DateTime,
        instrument.Symbol,
        Position.Transactions.Last.Price,
        Position.Transactions.Last.Cost,
        Position.Transactions.Last.Amount,
        Position.Transactions.Last.Value,
        Position.Transactions.Last.CashFlow,
        Position.Transactions.Last.NetCashFlow
      );
      */

      base.OnPositionOpened();
      //gpstrategy.HasPosition = true;
    }

    public override void OnPositionClosed() {
      //		Console.WriteLine( "{0} Position Closed",instrument.Symbol );
      base.OnPositionClosed();
      //gpstrategy.HasPosition = false;
    }

    public override void OnOrderFilled( SingleOrder order ) {
      base.OnOrderFilled(order);
      /*
      Console.WriteLine( "{0} {1} Order Filled", order.TransactTime, instrument.Symbol );
      Portfolio p = order.Portfolio;
      Console.WriteLine( "{0} {1} Order Info {2:#.00} {3:#.00} {4:#.00} {5:#.00} {6:#.00} {7:#.00}",
      p.Transactions.Last.DateTime,
      instrument.Symbol,
      p.Transactions.Last.Price,
      p.Transactions.Last.Cost,
      p.Transactions.Last.Amount,
      p.Transactions.Last.Value,
      p.Transactions.Last.CashFlow,
      p.Transactions.Last.NetCashFlow
      );
      */
    }

    public override void OnPositionChanged() {
      //		Console.WriteLine( "{0} Position Changed", instrument.Symbol );
    }

    public override void OnPositionValueChanged() {
      //		Console.WriteLine( "{0} Position Value Changed.", instrument.Symbol );
    }

    public override void OnStrategyStop() {
      //Console.WriteLine( "OnStrategyStop");

    }


  }






  #region PriceNode

  public class PriceNode : DoubleNode {
    // base
    // output one double
    // input one double
    static PriceNode() {
      PriceTradeNode ptn = new PriceTradeNode();
      ptn = null;
      PriceBidNode pbn = new PriceBidNode();
      pbn = null;
      PriceAskNode pan = new PriceAskNode();
      pan = null;
      PriceBidAskMidPointNode bamp = new PriceBidAskMidPointNode();
      bamp = null;
      //PriceOpenNode pon = new PriceOpenNode();
      //pon = null;
      //PriceCloseNode pcn = new PriceCloseNode();
      //pcn = null;
      //PriceHighNode phn = new PriceHighNode();
      //pcn = null;
      //PriceLowNode pln = new PriceLowNode();
      //pln = null;
      PriceKAMA0Node pkn0 = new PriceKAMA0Node();
      pkn0 = null;
      PriceKAMA1Node pkn1 = new PriceKAMA1Node();
      pkn1 = null;
      PriceSmaBidAskNode basn = new PriceSmaBidAskNode();
      basn = null;
      //PriceSkewNode skew = new PriceSkewNode();
      //skew = null;
      //PriceKurtosisNode kurtosis = new PriceKurtosisNode();
      //kurtosis = null;

    }

    public PriceNode() {
      Terminal = true;
      cntNodes = 0;
    }

    public override double EvaluateDouble( object o ) {
      throw new NotSupportedException("Can not call EvaluateDouble on void base.");
    }

  }

  public class PriceTradeNode : PriceNode {
    // output one double
    // no input

    static PriceTradeNode() {
      alDoubleNodes.Add(typeof(PriceTradeNode));
    }

    public override string ToString() {
      return "dsTrade.Last";
    }

    public override double EvaluateDouble( object o ) {

      if ((o as atscQT).dsTrade.Count > 0) {
        return (o as atscQT).dsTrade.Last;
      }
      else return (o as atscQT).Default;

    }
  }

  public class PriceBidNode : PriceNode {
    // output one double
    // no input

    static PriceBidNode() {
      alDoubleNodes.Add(typeof(PriceBidNode));
    }

    public override string ToString() {
      return "dsBid.Last";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsBid.Count > 0) {
        return (o as atscQT).dsBid.Last;
      }
      else return (o as atscQT).Default;
    }
  }

  public class PriceAskNode : PriceNode {
    // output one double
    // no input

    static PriceAskNode() {
      alDoubleNodes.Add(typeof(PriceAskNode));
    }

    public override string ToString() {
      return "dsAsk.Last";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsAsk.Count > 0) {
        return (o as atscQT).dsAsk.Last;
      }
      else return (o as atscQT).Default;
    }
  }

  public class PriceBidAskMidPointNode : PriceNode {
    // output one double
    // no input

    static PriceBidAskMidPointNode() {
      alDoubleNodes.Add(typeof(PriceBidAskMidPointNode));
    }

    public override string ToString() {
      return "dsBidAskMidPoint.Last";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsBidAskMidPoint.Count > 0) {
        return (o as atscQT).dsBidAskMidPoint.Last;
      }
      else return (o as atscQT).Default;
    }
  }

  /*
  public class PriceOpenNode : PriceNode {
    // output one double
    // no input

    static PriceOpenNode() {
      alDoubleNodes.Add(typeof(PriceOpenNode));
    }

    public override string ToString() {
      return "bars.Last.Open";
    }

    public override double EvaluateDouble( GPStrategy strategy ) {
      if (strategy.bars.Count > 0) {
        return strategy.bars.Last.Open;
      }
      else return strategy.Default;

    }
  }

  public class PriceCloseNode : PriceNode {
    // output one double
    // no input

    static PriceCloseNode() {
      alDoubleNodes.Add(typeof(PriceCloseNode));
    }

    public override string ToString() {
      return "bars.Last.Close";
    }

    public override double EvaluateDouble( GPStrategy strategy ) {
      if (strategy.bars.Count > 0) {
        return strategy.bars.Last.Close;
      }
      else return strategy.Default;
    }
  }

  public class PriceHighNode : PriceNode {
    // output one double
    // no input

    static PriceHighNode() {
      alDoubleNodes.Add(typeof(PriceHighNode));
    }

    public override string ToString() {
      return "bars.Last.High";
    }

    public override double EvaluateDouble( GPStrategy strategy ) {
      if (strategy.bars.Count > 0) {
        return strategy.bars.Last.High;
      }
      else return strategy.Default;
    }
  }

  public class PriceLowNode : PriceNode {
    // output one double
    // no input

    static PriceLowNode() {
      alDoubleNodes.Add(typeof(PriceLowNode));
    }

    public override string ToString() {
      return "bars.Last.Low";
    }

    public override double EvaluateDouble( GPStrategy strategy ) {
      if (strategy.bars.Count > 0) {
        return strategy.bars.Last.Low;
      }
      else return strategy.Default;
    }
  }
   * */

  public class PriceKAMA0Node : PriceNode {
    // output one double
    // no input

    static PriceKAMA0Node() {
      alDoubleNodes.Add(typeof(PriceKAMA0Node));
    }

    public override string ToString() {
      return "dsBidAskKama0.Last";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsBidAskKama0.Count > 0) {
        return (o as atscQT).dsBidAskKama0.Last;
      }
      else return (o as atscQT).Default;
    }
  }

  public class PriceKAMA1Node : PriceNode {
    // output one double
    // no input

    static PriceKAMA1Node() {
      alDoubleNodes.Add(typeof(PriceKAMA1Node));
    }

    public override string ToString() {
      return "dsBidAskKama1.Last";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsBidAskKama1.Count > 0) {
        return (o as atscQT).dsBidAskKama1.Last;
      }
      else return (o as atscQT).Default;
    }
  }

  public class PriceSkewNode : PriceNode {
    // output one double
    // no input

    static PriceSkewNode() {
      alDoubleNodes.Add(typeof(PriceSkewNode));
    }

    public override string ToString() {
      return "dsSkew.Last";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsSkew.Count > 0) {
        return (o as atscQT).dsSkew.Last;
      }
      else return (o as atscQT).Default;
    }
  }

  public class PriceKurtosisNode : PriceNode {
    // output one double
    // no input

    static PriceKurtosisNode() {
      alDoubleNodes.Add(typeof(PriceKurtosisNode));
    }

    public override string ToString() {
      return "dsKurtosis.Last";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsKurtosis.Count > 0) {
        return (o as atscQT).dsKurtosis.Last;
      }
      else return (o as atscQT).Default;
    }
  }

  public class PriceSmaBidAskNode : PriceNode {
    // output one double
    // no input

    static PriceSmaBidAskNode() {
      alDoubleNodes.Add(typeof(PriceSmaBidAskNode));
    }

    public override string ToString() {
      return "smaBidAskMidPoint.Last";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).smaBidAskMidPoint.Count > 0) {
        return (o as atscQT).smaBidAskMidPoint.Last;
      }
      else return (o as atscQT).Default;
    }
  }

  #endregion PriceNode

  #region AgoNode

  public class AgoNode : DoubleNode {
    // base
    // output one double
    // input one double

    protected int Index;

    static AgoNode() {
      AgoTradeNode ptn = new AgoTradeNode();
      ptn = null;
      AgoBidNode pbn = new AgoBidNode();
      pbn = null;
      AgoAskNode pan = new AgoAskNode();
      pan = null;
      AgoBidAskMidPointNode bamp = new AgoBidAskMidPointNode();
      bamp = null;
      /*
      AgoOpenNode pon = new AgoOpenNode();
      pon = null;
      AgoCloseNode pcn = new AgoCloseNode();
      pcn = null;
      AgoHighNode phn = new AgoHighNode();
      pcn = null;
      AgoLowNode pln = new AgoLowNode();
      pln = null;
       */
      AgoKAMA0Node pkn = new AgoKAMA0Node();
      pkn = null;
      AgoSmaBidAskNode basn = new AgoSmaBidAskNode();
      basn = null;
    }

    public AgoNode() {
      Terminal = true;
      cntNodes = 0;
      Index = random.Next(1, 50);
    }

    public AgoNode( int Index ) {
      Terminal = true;
      cntNodes = 0;
      this.Index = Index;
    }

    protected override void CopyValuesTo( Node node ) {
      // copy this.values to replicated copy
      AgoNode ago = node as AgoNode;
      ago.Index = this.Index;
    }

    public override double EvaluateDouble( object o ) {
      throw new NotSupportedException("Can not call EvaluateDouble on void base.");
    }

  }

  public class AgoTradeNode : AgoNode {
    // output one double
    // no input

    static AgoTradeNode() {
      alDoubleNodes.Add(typeof(AgoTradeNode));
    }

    public override string ToString() {
      return "dsTrade.Ago(" + Index.ToString() + ")";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsTrade.Count > Index) {
        return (o as atscQT).dsTrade.Ago(Index);
      }
      else return (o as atscQT).Default;

    }
  }

  public class AgoBidNode : AgoNode {
    // output one double
    // no input

    static AgoBidNode() {
      alDoubleNodes.Add(typeof(AgoBidNode));
    }

    public override string ToString() {
      return "dsBid.Ago(" + Index.ToString() + ")";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsBid.Count > Index) {
        return (o as atscQT).dsBid.Ago(Index);
      }
      else return (o as atscQT).Default;
    }
  }

  public class AgoAskNode : AgoNode {
    // output one double
    // no input

    static AgoAskNode() {
      alDoubleNodes.Add(typeof(AgoAskNode));
    }

    public override string ToString() {
      return "dsAsk.Ago(" + Index.ToString() + ")";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsAsk.Count > Index) {
        return (o as atscQT).dsAsk.Ago(Index);
      }
      else return (o as atscQT).Default;
    }
  }

  public class AgoBidAskMidPointNode : AgoNode {
    // output one double
    // no input

    static AgoBidAskMidPointNode() {
      alDoubleNodes.Add(typeof(AgoBidAskMidPointNode));
    }

    public override string ToString() {
      return "dsBidAskMidPoint.Ago(" + Index.ToString() + ")";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsBidAskMidPoint.Count > Index) {
        return (o as atscQT).dsBidAskMidPoint.Ago(Index);
      }
      else return (o as atscQT).Default;
    }
  }

  /*
  public class AgoOpenNode : AgoNode {
    // output one double
    // no input

    static AgoOpenNode() {
      alDoubleNodes.Add(typeof(AgoOpenNode));
    }

    public AgoOpenNode() {
      Index = random.Next(1, 3);
    }

    public override string ToString() {
      return "bars.Ago(" + Index.ToString() + ").Open";
    }

    public override double EvaluateDouble( GPStrategy strategy ) {
      if (strategy.bars.Count > Index) {
        return strategy.bars.Ago(Index).Open;
      }
      else return strategy.Default;

    }
  }

  public class AgoCloseNode : AgoNode {
    // output one double
    // no input

    static AgoCloseNode() {
      alDoubleNodes.Add(typeof(AgoCloseNode));
    }

    public AgoCloseNode() {
      Index = random.Next(1, 3);
    }

    public override string ToString() {
      return "bars.Ago(" + Index.ToString() + ").Close";
    }

    public override double EvaluateDouble( GPStrategy strategy ) {
      if (strategy.bars.Count > Index) {
        return strategy.bars.Ago(Index).Close;
      }
      else return strategy.Default;
    }
  }

  public class AgoHighNode : AgoNode {
    // output one double
    // no input

    static AgoHighNode() {
      alDoubleNodes.Add(typeof(AgoHighNode));
    }

    public AgoHighNode() {
      Index = random.Next(1, 3);
    }

    public override string ToString() {
      return "bars.Ago(" + Index.ToString() + ").High";
    }

    public override double EvaluateDouble( GPStrategy strategy ) {
      if (strategy.bars.Count > Index) {
        return strategy.bars.Ago(Index).High;
      }
      else return strategy.Default;
    }
  }

  public class AgoLowNode : AgoNode {
    // output one double
    // no input

    static AgoLowNode() {
      alDoubleNodes.Add(typeof(AgoLowNode));
    }

    public AgoLowNode() {
      Index = random.Next(1, 3);
    }

    public override string ToString() {
      return "bars.Ago(" + Index.ToString() + ").Low";
    }

    public override double EvaluateDouble( GPStrategy strategy ) {
      if (strategy.bars.Count > Index) {
        return strategy.bars.Ago(Index).Low;
      }
      else return strategy.Default;
    }
  }
   */

  public class AgoKAMA0Node : AgoNode {
    // output one double
    // no input

    static AgoKAMA0Node() {
      alDoubleNodes.Add(typeof(AgoKAMA0Node));
    }

    public override string ToString() {
      return "dsBidAskKama0.Ago(" + Index.ToString() + ")";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).dsBidAskKama0.Count > Index) {
        return (o as atscQT).dsBidAskKama0.Ago(Index);
      }
      else return (o as atscQT).Default;
    }
  }

  public class AgoSmaBidAskNode : AgoNode {
    // output one double
    // no input

    static AgoSmaBidAskNode() {
      alDoubleNodes.Add(typeof(AgoSmaBidAskNode));
    }

    public override string ToString() {
      return "smaBidAskMidPoint.Ago(" + Index.ToString() + ")";
    }

    public override double EvaluateDouble( object o ) {
      if ((o as atscQT).smaBidAskMidPoint.Count > Index) {
        return (o as atscQT).smaBidAskMidPoint.Ago(Index);
      }
      else return (o as atscQT).Default;
    }
  }

  #endregion AgoNode

  #region VolumeNode

public class VolumeNode : DoubleNode {
  // base
  // output one double
  // input none

  static VolumeNode() {
    //alDoubleNodes.Add(typeof(VolumeNode));
  }

  public VolumeNode() {
    Terminal = true;
    cntNodes = 0;
  }

  public override string ToString() {
    return "Volume";
  }

  public override double EvaluateDouble( object o ) {
    if ((o as atscQT).dsTradeVolume.Count > 0) {
      return (o as atscQT).dsTradeVolume.Last;
    }
    else return (o as atscQT).Default;
  }
}

#endregion VolumeNode



//}
