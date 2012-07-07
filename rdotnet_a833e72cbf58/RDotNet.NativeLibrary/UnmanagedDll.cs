﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace RDotNet.NativeLibrary
{
	/// <summary>
	/// A proxy for unmanaged dynamic link library (DLL).
	/// </summary>
	[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
	public class UnmanagedDll : SafeHandle
	{
		public override bool IsInvalid
		{
			get { return IsClosed || handle == IntPtr.Zero; }
		}

		/// <summary>
		/// Creates a proxy for the specified dll.
		/// </summary>
		/// <param name="dllName">The DLL's name.</param>
		public UnmanagedDll(string dllName)
			: base(IntPtr.Zero, true)
		{
			if (dllName == null)
			{
				throw new ArgumentNullException("dllName");
			}
			if (dllName == string.Empty)
			{
				throw new ArgumentException("dllName");
			}

			IntPtr handle = LoadLibrary(dllName);
			if (handle == IntPtr.Zero)
			{
				throw new DllNotFoundException();
			}
			SetHandle(handle);
		}

		/// <summary>
		/// Creates the delegate function for the specified function defined in the DLL.
		/// </summary>
		/// <typeparam name="TDelegate">The type of delegate.</typeparam>
		/// <returns>The delegate.</returns>
		public TDelegate GetFunction<TDelegate>()
			where TDelegate : class
		{
			Type delegateType = typeof(TDelegate);
			if (!delegateType.IsSubclassOf(typeof(Delegate)))
			{
				throw new ArgumentException();
			}
			IntPtr function = GetFunctionAddress(handle, delegateType.Name);
			if (function == IntPtr.Zero)
			{
				throw new EntryPointNotFoundException();
			}
			return Marshal.GetDelegateForFunctionPointer(function, delegateType) as TDelegate;
		}

		/// <summary>
		/// Creates the delegate function for the specified function defined in the DLL.
		/// </summary>
		/// <typeparam name="TDelegate">The type of delegate.</typeparam>
		/// <param name="entryPoint">The name of function.</param>
		/// <returns>The delegate.</returns>
		public TDelegate GetFunction<TDelegate>(string entryPoint)
			where TDelegate : class
		{
			if (!typeof(TDelegate).IsSubclassOf(typeof(Delegate)))
			{
				throw new ArgumentException();
			}
			if (entryPoint == null)
			{
				throw new ArgumentNullException("entryPoint");
			}
			IntPtr function = GetFunctionAddress(handle, entryPoint);
			if (function == IntPtr.Zero)
			{
				throw new EntryPointNotFoundException();
			}
			return Marshal.GetDelegateForFunctionPointer(function, typeof(TDelegate)) as TDelegate;
		}

		/// <summary>
		/// Gets the handle of the specified entry.
		/// </summary>
		/// <param name="entryPoint">The name of function.</param>
		/// <returns>The handle.</returns>
		public IntPtr DangerousGetHandle(string entryPoint)
		{
			if (entryPoint == null)
			{
				throw new ArgumentNullException("entryPoint");
			}
			return GetFunctionAddress(handle, entryPoint);
		}

		protected override bool ReleaseHandle()
		{
			return FreeLibrary(handle);
		}

		protected override void Dispose(bool disposing)
		{
			if (FreeLibrary(handle))
			{
				SetHandleAsInvalid();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Adds a directory to the search path used to locate DLLs for the application.
		/// </summary>
		/// <remarks>
		/// Calls <c>SetDllDirectory</c> in the kernel32.dll on Windows.
		/// </remarks>
		/// <param name="dllDirectory">
		/// The directory to be added to the search path.
		/// If this parameter is an empty string (""), the call removes the current directory from the default DLL search order.
		/// If this parameter is NULL, the function restores the default search order.
		/// </param>
		/// <returns>If the function succeeds, the return value is nonzero.</returns>
		[Obsolete("Set environment variable 'PATH' instead.")]
#if UNIX
		public static bool SetDllDirectory(string dllDirectory)
		{
			if (dllDirectory == null)
			{
				System.Environment.SetEnvironmentVariable(LibraryPath, DefaultSearchPath, EnvironmentVariableTarget.Process);
			}
			else if (dllDirectory == string.Empty)
			{
				throw new NotImplementedException();
			}
			else
			{
				if (!Directory.Exists(dllDirectory))
				{
					return false;
				}
				string path = System.Environment.GetEnvironmentVariable(LibraryPath, EnvironmentVariableTarget.Process);
				if (string.IsNullOrEmpty(path))
				{
					path = dllDirectory;
				}
				else
				{
					path = dllDirectory + Path.PathSeparator + path;
				}
				System.Environment.SetEnvironmentVariable(LibraryPath, path, EnvironmentVariableTarget.Process);
			}
			return true;
		}

		private const string LibraryPath = "PATH";
		private static readonly string DefaultSearchPath = System.Environment.GetEnvironmentVariable(LibraryPath, EnvironmentVariableTarget.Process);
#else
		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetDllDirectory([MarshalAs(UnmanagedType.LPStr)] string dllDirectory);
#endif

#if UNIX
		private static IntPtr LoadLibrary(string filename)
		{
			const int RTLD_LAZY = 0x1;
			if (filename.StartsWith("/"))
			{
				return dlopen(filename, RTLD_LAZY);
			}
			var searchPaths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
			var dll = searchPaths.Select(directory => Path.Combine(directory, filename)).FirstOrDefault(File.Exists);
			return dll == null ? IntPtr.Zero : dlopen(dll, RTLD_LAZY);
		}
		
		[DllImport("libdl")]
		private static extern IntPtr dlopen([MarshalAs(UnmanagedType.LPStr)] string filename, int flag);
#else
		[DllImport("kernel32.dll")]
		private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);
#endif

#if UNIX
		[DllImport("libdl", EntryPoint = "dlclose")]
#else
		[DllImport("kernel32.dll")]
#endif
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[return : MarshalAs(UnmanagedType.Bool)]
		private static extern bool FreeLibrary(IntPtr hModule);

#if UNIX
		[DllImport("libdl", EntryPoint = "dlsym")]
#else
		[DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
#endif
		private static extern IntPtr GetFunctionAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);
	}
}
