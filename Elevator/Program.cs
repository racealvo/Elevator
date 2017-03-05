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
                    currentFloor = (NumberOfFloorsInBuilding == 0) ? 0 : NumberOfFloorsInBuilding - 1;
                }
                else if (value < 0)
                {
                    currentFloor = 0;
                }
                else
                {
                    currentFloor = value;
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
        /// </summary>
        /// <param name="floor"></param>
        /// <param name="direction"></param>
        public void Call(int floor, Direction requestedDirection)
        {
            if (ElevatorDirection == Direction.idle)
            {
                FutureDirection = requestedDirection;
            }

            AddFloorToList(floor, requestedDirection);
            //Console.WriteLine("{0}: {1}", requestedDirection.ToString(), floor);
        }

        /// <summary>
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

                    ElevatorDirection = FutureDirection;
                }
            }


            if (directionList.Contains(CurrentFloor))
            {
                Console.WriteLine("Car is on floor {0}, loading/unloading passengers, and {1}.", CurrentFloor, (ElevatorDirection == Direction.idle) ? "is idle" : "is heading " + ElevatorDirection.ToString());
                Console.WriteLine("debug: you should not see idle here.");
                directionList.Remove(CurrentFloor);
            }

            if (upList.Count == 0 && downList.Count == 0)
            {
                ElevatorDirection = Direction.idle;
                Console.WriteLine("The elevator is idle.");
            }
            else
            {
                // Move the elevator 
                Console.WriteLine("The elevator is moving past floor {0} transitioning to {1}", CurrentFloor, directionList.Find((n) => n > CurrentFloor));
                CurrentFloor = (ElevatorDirection == Direction.up) ? CurrentFloor + 1 : CurrentFloor - 1;
            }
        }

        private void RequestFromCar(int floor)
        {
            if (((ElevatorDirection == Direction.up) && (floor > CurrentFloor)) ||
                ((ElevatorDirection == Direction.down) && (floor < CurrentFloor)))
            {
                AddFloorToList(floor, ElevatorDirection);
            }
            else if (ElevatorDirection == Direction.idle)
            {
                AddFloorToList(floor, FutureDirection);
            }
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
        public bool ProcessInput()
        {
            bool proceed = true;
            int floor = -1;
            Direction direction = Direction.idle;
            string input = string.Empty;

            try
            {
                // Wait for user input.
                input = Console.ReadLine();

                input = input.ToUpper();
                if (input == "EXIT")
                {
                    throw (new Exception());
                }

                floor = (int)Char.GetNumericValue(input[1]);
                switch (input[0])
                {
                    case 'P':
                        RequestFromCar(floor);
                        break;
                    case 'U':
                        direction = Direction.up;
                        Call(floor, direction);
                        break;
                    case 'D':
                        direction = Direction.down;
                        Call(floor, direction);
                        break;
                    default:
                        throw (new Exception());
                }
            }
            catch (Exception ex)
            {
                proceed = false;
            }

            return proceed;
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

            do
            {
                proceed = elevator.ProcessInput();
            } while (proceed);

            Console.WriteLine("Terminating Application.\nGoodbye.");
        }
    }
}
