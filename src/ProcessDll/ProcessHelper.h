#ifndef __PROCESS_HELPER_H__
#define __PROCESS_HELPER_H__
#pragma once

#include <Windows.h>
#include <winternl.h>

#define ProcessVmCounters ((PROCESSINFOCLASS)3)

typedef struct _VM_COUNTERS_EX2 {
	SIZE_T PeakVirtualSize;
	SIZE_T VirtualSize;
	ULONG PageFaultCount;
	SIZE_T PeakWorkingSetSize;
	SIZE_T WorkingSetSize;
	SIZE_T QuotaPeakPagedPoolUsage;
	SIZE_T QuotaPagedPoolUsage;
	SIZE_T QuotaPeakNonPagedPoolUsage;
	SIZE_T QuotaNonPagedPoolUsage;
	SIZE_T PagefileUsage;
	SIZE_T PeakPagefileUsage;
	SIZE_T PrivateUsage;
	SIZE_T PrivateWorkingSetSize;
	SIZE_T SharedCommitUsage;
} VM_COUNTERS_EX2, * PVM_COUNTERS_EX2;

class CProcessHelper {
public:
	static bool GetProcessCommandLine(DWORD dwProcId, LPWSTR& szCmdLine);
	static long GetPrivateWorkingSetSize(DWORD dwProcId);

private:
	static PVOID GetPebAddress(HANDLE ProcessHandle);

	typedef NTSTATUS(NTAPI* _NtQueryInformationProcess)(
		HANDLE ProcessHandle,
		DWORD ProcessInformationClass,
		PVOID ProcessInformation,
		DWORD ProcessInformationLength,
		PDWORD ReturnLength);
};

#endif /** !__PROCESS_HELPER_H__ **/
