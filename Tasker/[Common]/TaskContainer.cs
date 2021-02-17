namespace Tasker
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Security.Cryptography;

    using Tasker.Common.Task;

    public class TaskContainer : ITaskContainer, IConfigContainer
    {
        #region Fields

        private Dictionary<int, TaskCommon> _container;

        #endregion Fields

        #region Constructors

        public TaskContainer() 
        {
            _container = new Dictionary<int, TaskCommon>();
        }

        #endregion Constructors

        #region Methods

        public bool TryGet(int subjectId, string taskId, out TaskCommon taskCommon) 
        {
            return _container.TryGetValue(GetKey(subjectId, taskId), out taskCommon);
        }

        public void Set(IReadOnlyDictionary<int, string> subjectToTask, TaskContext context)
        {
            foreach (KeyValuePair<int, string> source in subjectToTask)
            {
                if (string.IsNullOrWhiteSpace(source.Value))
                {
                    continue;
                }

                foreach (KeyValuePair<int, string> target in subjectToTask)
                {
                    if (source.Key == target.Key || string.IsNullOrWhiteSpace(target.Value))
                    {
                        continue;
                    }

                    int keySource = GetKey(source.Key, target.Value);
                    int keyTarget = GetKey(target.Key, source.Value);

                    _container[keySource] = new TaskCommon { ExternalId = source.Value, Context = context };
                    _container[keyTarget] = new TaskCommon { ExternalId = target.Value, Context = context };
                }
            }
        }

        private static int GetKey(int subjectId, string taskId)
        {
            using var sha = MD5.Create();

            int hash = 17;
            hash = hash * 31 + BitConverter.ToInt32(sha.ComputeHash(Encoding.UTF8.GetBytes(subjectId.ToString())), 0);
            hash = hash * 31 + BitConverter.ToInt32(sha.ComputeHash(Encoding.UTF8.GetBytes(taskId)), 0);

            return hash;
        }

        public async Task<IConfigContainer> Load(IConfigProvider provider, CancellationToken token = default)
        {
            _container = new Dictionary<int, TaskCommon>(await provider.Read<List<KeyValuePair<int, TaskCommon>>>(this, token));
            return this;
        }

        public async Task<IConfigContainer> Save(IConfigProvider provider, CancellationToken token = default)
        {
            await provider.Write(this, _container.ToList(), token);
            return this;
        }

        #endregion Methods
    }
}
