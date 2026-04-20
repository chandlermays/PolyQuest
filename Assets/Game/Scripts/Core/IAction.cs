/*---------------------------
File: IAction.cs
Author: Chandler Mays
----------------------------*/
namespace PolyQuest.Core
{
    public interface IAction
    {
        //        void Execute();       can we try some sort of IAction<T> where Execute can take in params of (T obj)?
        //                              the current systems have their own method of StartAction that takes in some data parameter it uses.
        //                              For instance, PlayerController calls MovementComponent.StartMoveAction passing in a Vector3 'destination'.
        //                              Perhaps we could do Execute(Vector3 destination) instead?
        //                              With the Combat Component, it calls SetTarget passing in a GameObject 'target', so it would be Execute(GameObject target) in that case.

        void Cancel();
        //.. anything else?
    }
}


// April 20th: Return to this concern for using Execute() or just leaving it out.