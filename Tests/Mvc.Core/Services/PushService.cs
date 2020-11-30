using Grpc.Core;
using System.Threading.Tasks;

namespace Mvc.Core
{
    /// <summary>
    /// ���ͷ���
    /// </summary>
    public class PushService : Push.PushBase
    {
        /// <summary>
        /// ���ơ�
        /// </summary>
        /// <param name="request">������Ϣ��</param>
        /// <param name="context">�����ġ�</param>
        /// <returns></returns>
        public override Task<PushResult> Push(PushRequest request, ServerCallContext context)
        {
            return new Task<PushResult>(() => new PushResult { Code = 200 });
        }
    }
}
