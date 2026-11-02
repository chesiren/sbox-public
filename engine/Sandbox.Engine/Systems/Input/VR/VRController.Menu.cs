// FORK-VR: file added by the fork, absent from upstream
namespace Sandbox.VR;

partial record VRController
{
	/// <summary>
	/// The menu button on this controller. On Oculus Touch it's the left hand's menu button; the right hand's
	/// is the system button, which the runtime usually keeps for itself.
	/// </summary>
	public DigitalInput ButtonMenu { get; internal set; }
}
