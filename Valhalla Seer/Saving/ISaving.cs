using System.Threading.Tasks;

namespace Valhalla_Seer.Saving
{
    public interface ISavable
    {
        object CaptureState();
        Task RestoreState(object state);
    }
}
