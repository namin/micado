// This is the main DLL file.

#include "stdafx.h"

#include "MgCS2.h"

template<class T>
inline T ABS( const T x )
{
 return( x >= T( 0 ) ? x : -x );
 }

MgCS2::MgMCFSolver::MgMCFSolver(unsigned int pn, unsigned int pm, array<double>^ pUA, array<double>^ pCA, array<double>^ pDfctA, array<unsigned int>^ pSnA, array<unsigned int>^ pEnA)
{
	double* pU = new double[ pm ];
	double* pC = new double[ pm ];
	double* pDfct = new double[ pn ];
	unsigned int* pSn = new unsigned int[ pm ];
	unsigned  int* pEn = new unsigned int[ pm ];
	for (unsigned int i=0; i < pm; i++)
	{
		pU[i] = pUA[i];
		pC[i] = pCA[i];
		pSn[i] = pSnA[i];
		pEn[i] = pEnA[i];
	}
	for (unsigned int i=0; i < pn; i++)
	{
		pDfct[i] = pDfctA[i];
	}
	MgMCFSolver::initMgMCFSolver(pn, pm, pU, pC, pDfct, pSn, pEn);
}

void MgCS2::MgMCFSolver::initMgMCFSolver(unsigned int pn, unsigned int pm, double pU[], double pC[], double pDfct[], unsigned int pSn[], unsigned int pEn[])
{
	MgCS2::MgMCFSolver::mcf = new CS2(pn, pm);
	mcf->LoadNet(pn, pm, pn, pm, pU, pC, pDfct, pSn, pEn);
   #if( EPS_FLOW && ( Ftype == REAL_TYPE ) )
   FNumber eF = 1;
   for( register Index i = mcf->MCFm() ; i-- ; )
    eF = max( eF , ABS( mcf->MCFUCap( i ) ) );

   for( register Index i = mcf->MCFn() ; i-- ; )
    eF = max( eF , ABS( mcf->MCFDfct( i ) ) );

   mcf->SetEpsFlw( F_em * eF * mcf->MCFm() * 10 );  // the epsilon for flows
  #endif

  #if( EPS_COST && ( Ctype == REAL_TYPE ) )
   CNumber eC = 1;
   for( register Index i = mcf->MCFm() ; i-- ; )
    eC = max( eC , ABS( mcf->MCFCost( i ) ) );

   mcf->SetEpsCst( C_em * eC * mcf->MCFm() * 10 );  // the epsilon for costs
  #endif
}

void MgCS2::MgMCFSolver::SolveMCF()
{
	mcf->SolveMCF();
}

bool MgCS2::MgMCFSolver::HasSolution()
{
	return mcf->MCFGetStatus() == MCFClass::kOK;
}

void MgCS2::MgMCFSolver::MCFGetX(double x[])
{
	mcf->MCFGetX( x );
}

void MgCS2::MgMCFSolver::MCFGetX(array<double>^ xA)
{
    double* x = new double[ xA->Length ];
	MgMCFSolver::MCFGetX(x);
	for (int i=0; i<xA->Length; i++)
	{
		xA[i] = x[i];
	}
}