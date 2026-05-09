using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedSlugbase.Features;

//TODO: Determine testing enviornment and implementation
/* Dev tool menu which acts a lot like the remix menu: if a feature implements testing, allow the user to enter a live editing mode which updates the value as it's changed
 * Automated testing phase to ensure errors are properly caught and notified? 
 * Extremes are thoroughly tested to produce log results, and maybe temporary images for feature previews? (Mostly cosmetic things) */

/// <summary>
/// An interface for implementing live testing for features.
/// </summary>
public interface ITestFeatures<T>
{
	/// <summary>
	/// The <see cref="ProcessManager.ProcessID"/>s where the feature can be tested.
	/// </summary>
	public FeatureTesting.Setting Setting { get; }
}

public class FeatureTesting
{
	public enum Setting
	{
		SelectMenu,
		Game
	}
}
