// MgCS2.h

#pragma once

#include "CS2.h"

using namespace System;

namespace MgCS2 {

	public ref class MgMCFSolver
	{
	public:
		MgMCFSolver(unsigned int pn, unsigned int pm, array<double>^ pUA, array<double>^ pCA, array<double>^ pDfctA, array<unsigned int>^ pSnA, array<unsigned int>^ pEnA);
	    void SolveMCF();
		bool HasSolution();
		void MCFGetX(array<double>^ xA);

	private:
		void initMgMCFSolver(unsigned int pn, unsigned int pm, double pU[], double pC[], double pDfct[], unsigned int pSn[], unsigned int pEn[]);
		void MCFGetX(double x[]);

	private:
		MCFClass *mcf;
	};
}
