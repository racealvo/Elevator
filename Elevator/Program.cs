using System;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elevator
{
    public enum Direction
    {
        up, down, idle
    }

    public class Elevator
    {
        private List<int> upList;
        private List<int> downList;

        // This determines from which which list we service
        private Direction ElevatorDirection { get; set; }

        private int NumberOfFloorsInBuilding { get; }

        private static Direction FutureDirection { get; set; }

        // The elevator's current location
        private int currentFloor;
        private int CurrentFloor {
            get { return currentFloor; }
            set
            {
                if (value >= NumberOfFloorsInBuilding)
                {
                    currentFloor = NumberOfFloorsInBuilding - 1;
                }
                else if (value < 0)
                {
                    currentFloor = 0;
                }
            }
        }

        /// <summary>
        /// Need the number of floors in the building.  
        /// </summary>
        /// <param name="floors">number of foors in the building</param>
        /// <param name="currentFloor">currentFloor: default is 1</param>
        public Elevator(int floors, int currentFloor = 0)
        {
            ElevatorDirection = Direction.idle;
            FutureDirection = Direction.up;
            CurrentFloor = currentFloor;
            NumberOfFloorsInBuilding = floors;

            upList = new List<int>();
            downList = new List<int>();
        }

        /// <summary>
        /// This ensures we do not add same floor multiple times.
        /// </summary>
        /// <param name="floor"></param>
        /// <param name="direction"></param>
        private void AddFloorToList(int floor, Direction direction)
        {
            if (direction == Direction.up)
            {
                if (!upList.Contains(floor))
                {
                    upList.Add(floor);
                    upList.Sort();
                }
            }
            else if (direction == Direction.down)
            {
                if (!downList.Contains(floor))
                {
                    downList.Add(floor);
                    downList.Reverse();
                }
            }
        }

        /// <summary>
        /// Call from a floor.  
        /// If the elevator is idle, then set the elevator direction.
        /// Add true flag for the floor (array index) in the direction array.
        /// 
        /// The Call method should really be asynchronous to be able to add to a list anytime.
        /// </summary>
        /// <param name="floor"></param>
        /// <param name="direction"></param>
        public void Call(int floor, Direction requestedDirection)
        {
            if (ElevatorDirection == Direction.idle)
            {
                ElevatorDirection = requestedDirection;
            }

            AddFloorToList(floor, requestedDirection);
        }

        /// <summary>
        /// break to give users a chance to call on other floors.  
        /// This is a bit contrived.  The Call method should be asynchronous to be able to add to a list anytime.
        /// </summary>
        public void Proceed(object source, ElapsedEventArgs e)
        {
            List <int> directionList = null;

            if (ElevatorDirection == Direction.up)
            {
                directionList = upList;
                FutureDirection = Direction.down;
            }
            else if (ElevatorDirection == Direction.down)
            {
                directionList = downList;
                FutureDirection = Direction.up;
            }
            else // The elevator is idle - Get the future direction list, go to the floor at the beginning of that list - if it is populated
            {
                directionList = (FutureDirection == Direction.up) ? upList : downList;

                if (directionList.Count > 0)
                {
                    // Specificially 0, the directionList may actually contain the current floor.  
                    // However, someone may have called from another floor above/below.  
                    // 0 means this is the top/bottom of that list and noone else is above/below.
                    if (directionList[0] != CurrentFloor)
                    {
                        // Move the elevator 
                        CurrentFloor = (FutureDirection == Direction.up) ? CurrentFloor + 1 : CurrentFloor - 1;

                        Console.WriteLine("The elevator is moving past floor {0} transitioning to {1}", CurrentFloor, directionList[0]);
                        return;
                    }
                }
            }


            Console.WriteLine("Car on floor {0} and ", CurrentFloor, (ElevatorDirection == Direction.idle) ? "is idle." : "is heading " + ElevatorDirection.ToString());
/*
            // LEFT HERE
            if (directionList.Contains(CurrentFloor))
            {
                directionList.Remove(CurrentFloor);
                Console.WriteLine("Loaded passengers on floor {0}. Heading {1}.  Waiting for input from passenger.", CurrentFloor, ElevatorDirection.ToString());
            }

            // handle the reverse direction
*/
        }

        /// <summary>
        /// Get passenger floor input.  
        /// 
        /// </summary>
        /// <returns>If input is non-numeric, then return false.  The passenger opted not to press a floor.</returns>
        private bool PassengerInput()
        {
            bool goSomewhere = false;
            string input = string.Empty;
            int floor = -1;

            try
            {
                Console.WriteLine("You are on the elevator.  Please choose a destination floor.");

                // Wait for user input.  This elevator is not moving anywhere.
                // TODO: do this asynchronously
                input = Console.ReadLine();

                floor = (int)Char.GetNumericValue(input[0]);

                // Add floor only if it is valid.  On a real elevator - light the floor light - to acknowledge the user, but do not add the floor to the service list
                if ((ElevatorDirection == Direction.up && (floor > CurrentFloor)) ||
                    (ElevatorDirection == Direction.down && (floor < CurrentFloor)))
                {
                    AddFloorToList(floor, ElevatorDirection);
                    goSomewhere = true;
                }
            }
            catch (Exception ex)
            {
                // ignore any bad data and move onward.
            }

            return goSomewhere;
        }

        /// <summary>
        /// This method gets input from clients pushing the call button on a given floor.
        /// Hard wired input from physical devices are much less prone to error.  
        /// Since we are using a keyboard, this input could be flaky.  
        /// Bad data presumes the user wishes to terminate the appliction.
        /// 
        /// U for up
        /// D for down
        /// floors 0-9.  
        /// i.e. u6 - indicates a call from floor 6 requesting to go up. (case insensitive)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool ProcessInput(out int floor, out Direction direction)
        {
            bool proceed = true;
            floor = -1;
            direction = Direction.idle;
            string input = string.Empty;

            try
            {
                Console.WriteLine("Elevator is idle.");
                // Wait for user input.
                // TODO: do this asynchronously
                input = Console.ReadLine();

                input = input.ToUpper();
                if (input == "EXIT")
                {
                    throw (new Exception());
                }

                switch (input[0])
                {
                    case 'U':
                        direction = Direction.up;
                        break;
                    case 'D':
                        direction = Direction.down;
                        break;
                    default:
                        throw (new Exception());
                }

                floor = (int)Char.GetNumericValue(input[1]);
            }
            catch (Exception ex)
            {
                proceed = false;
            }

            return proceed;
        }

        public void HandleEvent(object sender, EventArgs args)
        {
            Console.WriteLine("We see there was some input here: {0}", args);
        }

        public async Task<string> GetInputAsync()
        {
            return await Task.Run(() => GatherInput());
        }

        private string GatherInput()
        {
            string input = Console.ReadLine();

            Console.WriteLine("User just added {0}", input);

            return input;
        }
    }

    class InputEvent : EventArgs
    {
        private readonly string input;

        public InputEvent(string s)
        {
            input = s;
        }

        public string Input()
        {
            return input;
        }
    }

    class Observable
    {
        public event EventHandler SomethingHappened;

        public void DoSomething()
        {
            string input = Console.ReadLine();
            EventHandler handler = SomethingHappened;
            if (handler != null)
            {
                handler(this, new InputEvent(input));
            }
        }
    }

    class Program
    {
        private static int NumberOfFloors = 10;

        static void Main(string[] args)
        {
            Elevator elevator = new Elevator(NumberOfFloors);
            int callFloor;
            Direction callDirection;
            bool proceed = true;

            Console.WriteLine("The building has 10 floors (0 - 9).  Enter d for down, u for up and a digit.  i.e. u1.  If you enter bad data, we figure you are done and the application will terminate.");

            Timer aTimer = new Timer();
            aTimer.Elapsed += new ElapsedEventHandler(elevator.Proceed);
            aTimer.Interval = 5000;
            aTimer.Enabled = true;

            Observable observable = new Observable();
            observable.SomethingHappened += elevator.HandleEvent;
            proceed = elevator.ProcessInput(out callFloor, out callDirection);

            while (proceed)
            {
                Console.WriteLine("{0}: {1}", callDirection.ToString().ToUpper(), callFloor);
                elevator.Call(callFloor, callDirection);
                //elevator.Proceed();
                proceed = elevator.ProcessInput(out callFloor, out callDirection);
            };

            Console.WriteLine("Terminating Application.\nGoodbye.");
        }
    }
}
