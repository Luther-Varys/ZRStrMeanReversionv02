using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class ZRStrMeanReversionv02 : Robot
    {
        [Parameter(DefaultValue = "Hello world!")]
        public string Message { get; set; }

        
        private ZRIndTrendDirection zRIndTrendDirection;
        private ZRindctraderZigZagRoboForex zRindctraderZigZagRoboForex;
        private ZRIndEngulfingCandle zrIndEngulfingCandle;
        
        private RelativeStrengthIndex relativeStrengthIndex;
        private double rsiMax = 70;
        private double rsiMin = 30;

        private const string POSTITON_LABEL_LONG = "mean reversion position LONG";
        private const string POSTITON_LABEL_SHORT = "mean reversion position SHORT";

        private DateTime DateTimeUtcStart { get; set; }

        protected override void OnStart()
        {
            System.Diagnostics.Debugger.Launch();
            zRIndTrendDirection = Indicators.GetIndicator<ZRIndTrendDirection>(21, 50, 200);
            zRindctraderZigZagRoboForex = Indicators.GetIndicator<ZRindctraderZigZagRoboForex>(this.Bars.HighPrices, this.Bars.LowPrices, 50, 2, "Black", "Green", "Red");
            zrIndEngulfingCandle = Indicators.GetIndicator<ZRIndEngulfingCandle>("Hello worldsss");
            relativeStrengthIndex = Indicators.RelativeStrengthIndex(MarketData.GetBars(TimeFrame.Hour).ClosePrices, 14);

            //DateTimeUtcStart = this.TimeInUtc.AddDays(35);
            DateTimeUtcStart = this.TimeInUtc.AddDays(1);

            Print(Message);
        }

        protected override void OnTick()
        {
            if(this.TimeInUtc < DateTimeUtcStart.ToUniversalTime())
                return;

            DirectionEnum positionDirection = GetPositionDirection();
            if (positionDirection != DirectionEnum.none)
                return;

            DirectionEnum trendDirectionBid = GetTrendDirection(Symbol.Bid);
            DirectionEnum trendDirectionAsk = GetTrendDirection(Symbol.Ask);
            if (trendDirectionBid == DirectionEnum.buy && trendDirectionAsk == DirectionEnum.buy)
            {
                Print("ZR: if (trendDirection == DirectionEnum.buy)");
                OpenPosition(TradeType.Buy);
            }
            else if (trendDirectionBid == DirectionEnum.sell && trendDirectionAsk == DirectionEnum.sell)
            {
                //Print("ZR: if (trendDirection == DirectionEnum.sell)");
                //OpenPosition(TradeType.Sell);
            }
        }

        private DirectionEnum GetTrendDirection(double price)
        {
            var trendH1 = GetPriceInRelationToEMATimeFrameH1(price);
            var trendH4 = GetPriceInRelationToEMATimeFrameH4(price);

            if (trendH1 == DirectionEnum.buy && trendH4 == DirectionEnum.buy)
                return DirectionEnum.buy;

            if (trendH1 == DirectionEnum.sell && trendH4 == DirectionEnum.sell)
                return DirectionEnum.sell;

            return DirectionEnum.none;

            //var trendH1 = GetTrendForTimeFrameH1();
            //var trendH4 = GetTrendForTimeFrameH4();

            //if (trendH1 == DirectionEnum.buy && trendH4 == DirectionEnum.buy)
            //    return DirectionEnum.buy;

            //if (trendH1 == DirectionEnum.sell && trendH4 == DirectionEnum.sell)
            //    return DirectionEnum.sell;

            //return DirectionEnum.none;
        }


        RsiTriggeredEnum rsiTriggered = RsiTriggeredEnum.none;
        bool isPositionOpened = false;
        protected void OnTickV01()
        {
            var resprsi = relativeStrengthIndex.Result.TakeLast(2).First();

            var positionDirection = GetPositionDirection();
            if(isPositionOpened == true && positionDirection == DirectionEnum.none)
            {
                rsiTriggered = RsiTriggeredEnum.none;
                isPositionOpened = false;
                return;
            }
            if (positionDirection != DirectionEnum.none)
            {
                Print("ZR: IsPositionOpened() == true");
                
                //if(positionDirection == DirectionEnum.buy && resprsi >= rsiMax)
                //{
                //    CloseAllPositions();
                //    rsiTriggered = RsiTriggeredEnum.none;
                //}
                //else if (positionDirection == DirectionEnum.sell && resprsi >= rsiMin)
                //{
                //    CloseAllPositions();
                //    rsiTriggered = RsiTriggeredEnum.none;
                //}

                return;
            }

            //if (rsiTriggered == RsiTriggeredEnum.none)
            {
                //var resprsi = relativeStrengthIndex.Result.TakeLast(2).First();
                if(resprsi >= rsiMax)
                {
                    rsiTriggered = RsiTriggeredEnum.high;
                }
                else if (resprsi <= rsiMin)
                {
                    rsiTriggered = RsiTriggeredEnum.low;
                }

                if(rsiTriggered == RsiTriggeredEnum.none)
                {
                    return;
                }
            }



            if (rsiTriggered == RsiTriggeredEnum.high)
            {
                //TODO: implement code
            }
            else if (rsiTriggered == RsiTriggeredEnum.low)
            {
                if (GetTrendForTimeFrameH4() != DirectionEnum.buy)
                    return;
                if (GetTrendForTimeFrameH1() != DirectionEnum.buy)
                    return;

                //Check ZigZag
                {
                    bool isZigZagSignalValid = IsZigZagSignalValid(DirectionEnum.buy);
                    var respEngulf = zrIndEngulfingCandle.Result.TakeLast(2).First();

                    //if (isZigZagSignalValid == true && respEngulf > 1)
                    if (respEngulf > 0.2)
                    {
                        Print("isZigZagSignalValid: TRUE");
                        OpenPosition(TradeType.Buy);
                    }
                    else
                    {
                        return;
                    }
                }
                //Check ZigZag Breakout
                //Wait for a red candle and wait for close of candle. Once closed open the position.
            }

        }

        private void OpenPosition(TradeType tradeType)
        {
            var position = Positions.Find(POSTITON_LABEL_LONG, SymbolName, tradeType);
            var volumeInUnits = Symbol.QuantityToVolumeInUnits(0.01);

            if (position == null)
            {
                //ExecuteMarketOrder(tradeType, SymbolName, volumeInUnits, POSTITON_LABEL_LONG, stopLossPips: 40, takeProfitPips: 80);
                ExecuteMarketOrder(tradeType, SymbolName, volumeInUnits, POSTITON_LABEL_LONG, stopLossPips: 15, takeProfitPips: 80);
                isPositionOpened = true;
            }
        }

        private (Position posLong, Position posShort) GetPosition()
        {
            var longPosition = Positions.Find(POSTITON_LABEL_LONG, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(POSTITON_LABEL_SHORT, SymbolName, TradeType.Sell);

            return (longPosition, shortPosition);
        }

        private void CloseAllPositions()
        {
            var longPosition = Positions.Find(POSTITON_LABEL_LONG, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(POSTITON_LABEL_SHORT, SymbolName, TradeType.Sell);

            if (longPosition != null)
                longPosition.Close();
            if (shortPosition != null)
                shortPosition.Close();
        }

        private DirectionEnum GetPositionDirection()
        {
            var longPosition = Positions.Find(POSTITON_LABEL_LONG, SymbolName, TradeType.Buy);
            var shortPosition = Positions.Find(POSTITON_LABEL_SHORT, SymbolName, TradeType.Sell);

            if (longPosition == null && shortPosition == null)
                return DirectionEnum.none;

            if(longPosition != null)
                return DirectionEnum.buy;
            if (shortPosition != null)
                return DirectionEnum.sell;

            return DirectionEnum.none;
        }

        private bool IsZigZagSignalValid(DirectionEnum trend)
        {
            int zzLastToTake = 4;
            if(trend == DirectionEnum.buy)
            {
                var resZigZag = zRindctraderZigZagRoboForex.Value.Where(x => x > 0).TakeLast(zzLastToTake + 1).Take(zzLastToTake).ToArray();
                if(resZigZag[0] > resZigZag[1] && resZigZag[1] < resZigZag[2] && resZigZag[2] > resZigZag[3])
                    return true;
                else
                    return false;
            }
            else if (trend == DirectionEnum.sell)
            {
                var resZigZag = zRindctraderZigZagRoboForex.Value.Where(x => x > 0).TakeLast(zzLastToTake + 1).Take(zzLastToTake).ToArray();
                if (resZigZag[0] < resZigZag[1] && resZigZag[1] > resZigZag[2] && resZigZag[2] < resZigZag[3])
                    return true;
                else
                    return false;
            }

            return false;
        }

        protected override void OnStop()
        {
            // Handle cBot stop here
        }


        private DirectionEnum GetTrendForTimeFrameM5()
        {
            if (zRIndTrendDirection == null)
                return DirectionEnum.none;

            if (zRIndTrendDirection.EMA_TF5M_P21 != null && zRIndTrendDirection.EMA_TF5M_P50 != null && zRIndTrendDirection.EMA_TF5M_P200 != null)
            {
                var respP21 = zRIndTrendDirection.EMA_TF5M_P21.Last();
                var respP50 = zRIndTrendDirection.EMA_TF5M_P50.Last();
                var respP200 = zRIndTrendDirection.EMA_TF5M_P200.Last();

                if(respP21 > respP50 && respP50 > respP200)
                {
                    return DirectionEnum.buy;
                }
                else if (respP21 < respP50 && respP50 < respP200)
                {
                    return DirectionEnum.sell;
                }
            }
            return DirectionEnum.none;
        }

        private DirectionEnum GetTrendForTimeFrameH1()
        {
            if (zRIndTrendDirection == null)
                return DirectionEnum.none;

            if (zRIndTrendDirection.EMA_TF1H_P21 != null && zRIndTrendDirection.EMA_TF1H_P50 != null && zRIndTrendDirection.EMA_TF1H_P200 != null)
            {
                var respP21 = zRIndTrendDirection.EMA_TF1H_P21.Last();
                var respP50 = zRIndTrendDirection.EMA_TF1H_P50.Last();
                var respP200 = zRIndTrendDirection.EMA_TF1H_P200.Last();

                if (respP21 > respP50 && respP50 > respP200)
                {
                    return DirectionEnum.buy;
                }
                else if (respP21 < respP50 && respP50 < respP200)
                {
                    return DirectionEnum.sell;
                }
            }
            return DirectionEnum.none;
        }

        private DirectionEnum GetTrendForTimeFrameH4()
        {
            if (zRIndTrendDirection == null)
                return DirectionEnum.none;

            if (zRIndTrendDirection.EMA_TF4H_P21 != null && zRIndTrendDirection.EMA_TF4H_P50 != null && zRIndTrendDirection.EMA_TF4H_P200 != null)
            {
                var respP21 = zRIndTrendDirection.EMA_TF4H_P21.Last();
                var respP50 = zRIndTrendDirection.EMA_TF4H_P50.Last();
                var respP200 = zRIndTrendDirection.EMA_TF4H_P200.Last();

                if (respP21 > respP50 && respP50 > respP200)
                {
                    return DirectionEnum.buy;
                }
                else if (respP21 < respP50 && respP50 < respP200)
                {
                    return DirectionEnum.sell;
                }
            }
            return DirectionEnum.none;
        }

        private DirectionEnum GetPriceInRelationToEMATimeFrameH1(double price)
        {
            var respP21 = zRIndTrendDirection.EMA_TF1H_P21.Last();
            var respP50 = zRIndTrendDirection.EMA_TF1H_P50.Last();
            var respP200 = zRIndTrendDirection.EMA_TF1H_P200.Last();

            if(price > respP21 && price > respP50 && price > respP200)
            {
                return DirectionEnum.buy;
            }
            else if (price < respP21 && price < respP50 && price < respP200)
            {
                return DirectionEnum.sell;
            }

            return DirectionEnum.none;
        }

        private DirectionEnum GetPriceInRelationToEMATimeFrameH4(double price)
        {
            var respP21 = zRIndTrendDirection.EMA_TF4H_P21.Last();
            var respP50 = zRIndTrendDirection.EMA_TF4H_P50.Last();
            var respP200 = zRIndTrendDirection.EMA_TF4H_P200.Last();

            if (price > respP21 && price > respP50 && price > respP200)
            {
                return DirectionEnum.buy;
            }
            else if (price < respP21 && price < respP50 && price < respP200)
            {
                return DirectionEnum.sell;
            }

            return DirectionEnum.none;
        }

    }

    public enum AboveBelowEnum
    {
        above,
        below,
        none
    }

    public enum RsiTriggeredEnum
    {
        high,
        low,
        none
    }

    public enum DirectionEnum
    {
        buy,
        sell,
        none
    }
}