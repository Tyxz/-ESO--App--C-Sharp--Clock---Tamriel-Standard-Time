using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Clock.Data {
    class TST {
        private readonly long startTime = 1396569600; // s

        private readonly double lengthOfDay; // s 
        private readonly double lengthOfHour; // s 
        private readonly double lengthOfMinute; // s 
        private readonly double lengthOfSecond; // s 
        private readonly double timeOfSyncMidnight; // s

        private readonly int lengthOfCycle = 628650; // 30 * lengthOfDay
        private readonly float lengthOfFull = 62865; // 10%
        private readonly float lengthOfNew = 31432.5f; // 5%
        private readonly float lengthOfWay = 267176.25f; // 42.5%

        private readonly int[] monthLengths = {
            -1, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };
        public enum MoonState {
            New = 0,
            Waxing = 1,
            Full = 2,
            Wanning = 3
        }
        
        public int Era = 2;
        public int Year = 582;
        public int Month = 4;
        public int Day = 4;
        public int Hour = 0;
        public int Minute = 0;
        public int Second = 0;
        public MoonState moonState = MoonState.Full;
        public double moonWay = 0;

        private double lastUpdate = 0;
        private double lastMoonUpdate = 0;
        private Timer timer;
        private Timer mooner;

        private Object l = new Object();
        private Object lm = new Object();

        public TST(double lengthOfDay = 20955, double timeOfSyncNoon = 1398044126, double timeOfSyncFull = 1425169441) {
            timeOfSyncMidnight = timeOfSyncNoon - lengthOfDay/2;
            this.lengthOfDay = lengthOfDay;
            lengthOfHour = lengthOfDay / 24;
            lengthOfMinute = lengthOfHour / 60;
            lengthOfSecond = lengthOfMinute / 60;

            double timeSinceStart = timeOfSyncMidnight - startTime;
            lastUpdate = timeOfSyncMidnight;

            CalculateTST(timeSinceStart);
            Hour = 0;
            Minute = 0;
            Second = 0;

            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 0.001d;
            double timeSinceLastUpdate = now - timeOfSyncFull;
            lastMoonUpdate = now;
            CalculateMoon(timeSinceLastUpdate);
        }

        public override string ToString() {
            return Math.Round(moonWay, 4) + " " + moonState + "::" + Day + "." + Month + "." + Era + "E" + Year + " " + Hour + ":" + Minute + ":" + Second;
        }

        public void StartClock() {
            try {

                timer = new Timer(100) {
                    AutoReset = true
                };
                timer.Elapsed += new ElapsedEventHandler(Update);
                timer.Enabled = true;
                timer.Start();

                mooner = new Timer(60000) {
                    AutoReset = true
                };
                mooner.Elapsed += new ElapsedEventHandler(Update);
                mooner.Enabled = true;
                mooner.Start();

            } catch (Exception ex) { }
        }

        public void StopClock() {
            try {
                timer.Stop();
                mooner.Stop();
            } catch (Exception ex) { }
        }

        private void Update(object sender, EventArgs eArgs) {
            lock (l) {
                double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 0.001d;
                double timeSinceLastUpdate = now - lastUpdate;
                if (timeSinceLastUpdate < lengthOfSecond)
                    return;
                lastUpdate = now;
                CalculateTST(timeSinceLastUpdate);
            }
        }

        private void UpdateMoon(object sender, EventArgs eArgs) {
            lock (lm) {
                double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 0.001d;
                double timeSinceLastUpdate = now - lastMoonUpdate;
                if (timeSinceLastUpdate < lengthOfHour)
                    return;
                lastMoonUpdate = now;
                CalculateMoon(timeSinceLastUpdate);
            }
        }

        private void CalculateTST(double offset) {

            int daysPast = (int)Math.Floor(offset / lengthOfDay);
            Day += daysPast;

            offset %= lengthOfDay;
            Hour += (int)Math.Floor(offset / lengthOfHour);
            offset %= lengthOfHour;
            Minute += (int)Math.Floor(offset / lengthOfMinute);
            offset %= lengthOfMinute;
            Second += (int)Math.Floor(offset / lengthOfSecond);

            while (Second > 59) {
                Minute++;
                Second -= 60;
            }

            while (Minute > 59) {
                Hour++;
                Minute -= 60;
            }
            while (Hour > 23) {
                Day++;
                Hour -= 24;
            }
            while (Day > monthLengths[Month]) {
                Day -= monthLengths[Month];
                Month++;
                if (Month > 12) {
                    Month = 1;
                    Year++;
                }
            }
        }

        private void CalculateMoon(double offset) {
            double waxing = lengthOfNew;
            double full = waxing + lengthOfWay;
            double wanning = full + lengthOfFull;

            switch (moonState) {
                case MoonState.Waxing:
                    offset += waxing;
                    offset += moonWay * lengthOfWay;
                    break;
                case MoonState.Full:
                    offset += full;
                    offset += moonWay * lengthOfFull;
                    break;
                case MoonState.Wanning:
                    offset += wanning;
                    offset += moonWay * lengthOfWay;
                    break;
                default:
                    offset += moonWay * lengthOfNew;
                    break;
            }

            while (offset >= lengthOfCycle)
                offset -= lengthOfCycle;

            if(offset >= wanning) {
                offset -= wanning;
                moonState = MoonState.Wanning;
                moonWay = offset / lengthOfWay;
            } else if(offset > full) {
                offset -= full;
                moonState = MoonState.Full;
                moonWay = offset / lengthOfFull;
            } else if (offset > waxing) {
                offset -= waxing;
                moonState = MoonState.Waxing;
                moonWay = offset / lengthOfWay;
            } else {
                moonState = MoonState.New;
                moonWay = offset / lengthOfNew;
            }
        }
    }
}
