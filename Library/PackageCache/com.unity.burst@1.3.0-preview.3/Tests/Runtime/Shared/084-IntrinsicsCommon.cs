using Burst.Compiler.IL.Tests.Helpers;
using static Unity.Burst.Intrinsics.Common;

namespace Burst.Compiler.IL.Tests
{
    internal class IntrinsicsCommon
    {
#if BURST_INTERNAL || UNITY_BURST_EXPERIMENTAL_PAUSE_INTRINSIC
        [TestCompiler]
        public static void CheckPause()
        {
            Pause();
        }
#endif

        [TestCompiler(typeof(ReturnBox), ulong.MaxValue, ulong.MaxValue)]
        [TestCompiler(typeof(ReturnBox), ulong.MinValue, ulong.MaxValue)]
        [TestCompiler(typeof(ReturnBox), ulong.MaxValue, ulong.MinValue)]
        [TestCompiler(typeof(ReturnBox), ulong.MinValue, ulong.MinValue)]
        [TestCompiler(typeof(ReturnBox), DataRange.Standard, DataRange.Standard)]
        public static unsafe ulong Checkumul128(ulong* high, ulong x, ulong y)
        {
            var result = umul128(x, y, out var myHigh);
            *high = myHigh;
            return result;
        }
    }
}
