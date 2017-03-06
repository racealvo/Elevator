﻿using System;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// This is a non-scalable version of an elevator program.  To scale it, we will have to make the upList and 
/// downList a global object, and deal with locking/unlocking sections of the Proceed method.
/// </summary>
namespace Elevator
{
    /// <summary>
    /// The only reason this is a static object is to be able to disable and re-enable the timer during debugging
    /// Set this value as a watched value.  If you disable it, be sure to enable it prior to resuming your program.
    /// Otherwise, the app will freeze.
    /// 
    /// ElevatorTimer.TheTimer.Enabled
    /// </summary>
    public static class ElevatorTimer
    {
        static Timer timer = new Timer();

        static public Timer TheTimer
        {
            get { return timer; }
        }
    }

    /// <summary>
    /// Define the elevator directions.
    /// </summary>
    public enum Direction
    {
        up, down, idle
    }

    /// <summary>
    /// The elevator class.  It does the (ahem) heavy lifting. 
    /// </summary>
    public class Elevator
    {
        private List<int> upList;
        private List<int> downList;

        // This determines from which which list we service
        private Direction CurrentDirection { get; set; }

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

        private Direction OppositeDirection
        {
            get
            {
                if (CurrentDirection == Direction.idle)
                    return Direction.idle;

                return (CurrentDirection == Direction.up) ? Direction.down : Direction.up;
            }
        }
        /// <summary>
        /// Need the number of floors in the building.  
        /// </summary>
        /// <param name="floors">number of foors in the building</param>
        /// <param name="currentFloor">currentFloor: default is 1</param>
        public Elevator(int floors, int currentFloor = 0)
        {
            CurrentDirection = Direction.idle;
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
            if (CurrentDirection == Direction.idle)
            {
                CurrentDirection = requestedDirection;
                FutureDirection = OppositeDirection;
            }

            AddFloorToList(floor, requestedDirection);
        }

        List<int> DirectionList()
        {
            List<int> directionList = null;

            if (CurrentDirection == Direction.up)
                directionList = upList;
            else if (CurrentDirection == Direction.down)
                directionList = downList;

            return directionList;
        }

        /// <summary>
        /// This method is called asynchronously.  It executes on a timer event.
        /// This method determines whether to stop on the current floor or proceed to the next floor.
        /// </summary>
        public void Proceed(object source, ElapsedEventArgs e)
        {
            List<int> directionList = null;

            if ((CurrentDirection == Direction.up) || (CurrentDirection == Direction.down))
            {
                directionList = DirectionList();
                FutureDirection = OppositeDirection;
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
                        Console.WriteLine("The elevator is moving past floor {0} transitioning to {1}", CurrentFloor, directionList.First());
                        return;
                    }

                    CurrentDirection = FutureDirection;
                }
            }


            if (directionList.Contains(CurrentFloor))
            {
                Console.WriteLine("Car is on floor {0}, loading/unloading passengers, and {1}.", CurrentFloor, (CurrentDirection == Direction.idle) ? "is idle" : "is heading " + CurrentDirection.ToString());
                Console.WriteLine("You have {0} seconds to choose your destination.  Otherwise, you may loose your turn.", ElevatorTimer.TheTimer.Interval / 1000);
                directionList.Remove(CurrentFloor);
                return;
            }

            // Set idle state
            if (upList.Count == 0 && downList.Count == 0)
            {
                CurrentDirection = Direction.idle;
                Console.WriteLine("The elevator is idle at floor {0}.", CurrentFloor);
                return;
            }

            // Reverse direction
            if ((CurrentDirection == Direction.up) && (upList.Count == 0) ||
                (CurrentDirection == Direction.down) && (downList.Count == 0))
            {
                CurrentDirection = OppositeDirection;
                directionList = DirectionList();
            }

            // Move the elevator - one of the lists is populated
            if (directionList != null)
            {
                int? found;

                // LEFT HERE - find is not right - want the null condition - so that we can reverse.
                if (CurrentDirection == Direction.up)
                {
                    found = upList.Find(f => f > CurrentFloor);
                }
                else
                {
                    found = downList.Find(f => f < CurrentFloor);
                }

                // We are at the end of the list going in this direction.
                if (found == null)
                {
                    List<int> originalList = DirectionList();
                    CurrentDirection = OppositeDirection;
                    directionList = DirectionList();

                    // are there any items in the current list?
                    if (directionList.Count == 0)
                    {
                        CurrentDirection = OppositeDirection;
                        found = originalList.First();
                    }
                    else
                    {
                        found = directionList.First();
                    }
                }

                Console.WriteLine("The elevator is moving past floor {0} transitioning to {1}", CurrentFloor, found);
                CurrentFloor = (CurrentDirection == Direction.up) ? CurrentFloor + 1 : CurrentFloor - 1;
            }
        }

        /// <summary>
        /// Conditionally add the floor request to a list.
        /// 
        /// If the car is heading up past floor 5 and the passenger hits 2, the floor is ignored.
        /// If the car is idle and responding to a floor request and happens to have a passenger
        /// </summary>
        /// <param name="floor"></param>
        private void RequestFromCar(int floor)
        {
            if (((CurrentDirection == Direction.up) && (floor > CurrentFloor)) ||
                ((CurrentDirection == Direction.down) && (floor < CurrentFloor)))
            {
                AddFloorToList(floor, CurrentDirection);
            }
            // This condition is most relevant to a building with a single elevator
            else if (((CurrentDirection == Direction.up) && (floor < CurrentFloor)) ||
                     ((CurrentDirection == Direction.down) && (floor > CurrentFloor)))
            {
                AddFloorToList(floor, OppositeDirection);
            }
            else if (CurrentDirection == Direction.idle)
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
        /// 
        /// P for passenger floor request within the elevator car
        /// i.e. p4 - indicates a passenger in the elevator pressed the 4 button. (case insensitive)
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

            //Timer aTimer = new Timer();
            ElevatorTimer.TheTimer.Elapsed += new ElapsedEventHandler(elevator.Proceed);
            ElevatorTimer.TheTimer.Interval = 5000;
            ElevatorTimer.TheTimer.Enabled = true;

            do
            {
                proceed = elevator.ProcessInput();
            } while (proceed);

            Console.WriteLine("Terminating Application.\nGoodbye.");
        }
    }
}
