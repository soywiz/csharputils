﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace CSharpUtils.Threading
{
	public class GreenThread
	{
		protected Action Action;

		protected Thread ParentThread;
		protected Thread CurrentThread;
		protected Semaphore ParentSemaphore;
		protected Semaphore ThisSemaphore;
		static protected ThreadLocal<GreenThread> ThisGreenThreadList = new ThreadLocal<GreenThread>();
		static public int GreenThreadLastId = 0;

		static public Thread MonitorThread;

		private Exception RethrowException;

		public bool Running { get; protected set; }

		public GreenThread()
		{
		}

		~GreenThread()
		{
		}

		void ThisSemaphoreWaitOrParentThreadStopped()
		{
			while (true)
			{
				// If the parent thread have been stopped. We should not wait any longer.
				if (!ParentThread.IsAlive) Thread.CurrentThread.Abort();

				if (ThisSemaphore.WaitOne(20))
				{
					// Signaled.
					break;
				}
			}
		}

		public void InitAndStartStopped(Action Action)
		{
			this.Action = Action;
			this.ParentThread = Thread.CurrentThread;

			ParentSemaphore = new Semaphore(1, 1);
			ParentSemaphore.WaitOne();

			ThisSemaphore = new Semaphore(1, 1);
			ThisSemaphore.WaitOne();

			var This = this;

			this.CurrentThread = new Thread(() =>
			{
				ThisGreenThreadList.Value = This;
				ThisSemaphoreWaitOrParentThreadStopped();
				try
				{
					Running = true;
					Action();
				}
				catch (Exception Exception)
				{
					RethrowException = Exception;
				}
				finally
				{
					ParentSemaphore.Release();
				}
			});

			this.CurrentThread.Name = "GreenThread-" + GreenThreadLastId++;

			this.CurrentThread.Start();
		}

		/// <summary>
		/// Called from the caller thread.
		/// This will give the control to the green thread.
		/// </summary>
		public void SwitchTo()
		{
			ParentThread = Thread.CurrentThread;
			ThisSemaphore.Release();
			ParentSemaphore.WaitOne();
			if (RethrowException != null)
			{
				try
				{
					//StackTraceUtils.PreserveStackTrace(RethrowException);
					throw (new GreenThreadException("GreenThread Exception", RethrowException));
					//throw (RethrowException);
				}
				finally
				{
					RethrowException = null;
				}
			}
		}

		/// <summary>
		/// Called from the green thread.
		/// This will return the control to the caller thread.
		/// </summary>
		static public void Yield()
		{
			if (ThisGreenThreadList.IsValueCreated)
			{
				var GreenThread = ThisGreenThreadList.Value;
				if (GreenThread.Running)
				{
					try
					{
						GreenThread.Running = false;
						GreenThread.ParentSemaphore.Release();
						GreenThread.ThisSemaphoreWaitOrParentThreadStopped();
					}
					finally
					{
						GreenThread.Running = true;
					}
				}
			}
		}

		static public void StopAll()
		{
			throw(new NotImplementedException());
		}

		public void Stop()
		{
			CurrentThread.Abort();
		}
	}
}
