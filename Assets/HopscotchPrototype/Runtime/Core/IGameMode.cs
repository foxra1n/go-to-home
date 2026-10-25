using System;

namespace Homeward.Hopscotch
{
    public interface IGameMode
    {
        event Action StepCompleted;
        event Action<string> Failed;
        void BeginRun();
        void SubmitTap();
        void Tick(float deltaTime);
    }

    public interface ISectionGameMode : IGameMode
    {
        event Action SectionCompleted;
        float EndZ { get; }
        void PrepareSection(float startZ, float duration);
        void StartSection();
        void EndRun();
    }
}
