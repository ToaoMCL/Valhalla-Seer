using System.Threading.Tasks;

namespace Valhalla_Seer.Saving
{
    public interface ISaving
    {
        object CaptureState();
        Task RestoreState(object state);
    }
}
