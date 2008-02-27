#include "StdAfx.h"

#if defined(_DEBUG) && !defined(AC_FULL_DEBUG)
#error _DEBUG should not be defined except in internal Adesk debug builds
#endif


//////////////////////////////////////////////////////////////////////////
#include <gcroot.h>
#include <dbdate.h>
#include "mgdinterop.h"



//////////////////////////////////////////////////////////////////////////
// constructor
BioStream::Valve::Valve() 
:Autodesk::AutoCAD::DatabaseServices::Polyline(new DbValve(), true)
{
	//acutPrintf(_T("\n*********************Constructor"));
}

//////////////////////////////////////////////////////////////////////////
BioStream::Valve::Valve(System::IntPtr unmanagedPointer, bool autoDelete)
: Autodesk::AutoCAD::DatabaseServices::Polyline(unmanagedPointer,autoDelete)
{
}

//////////////////////////////////////////////////////////////////////////
// set the center of the poly
void BioStream::Valve::set_Center(Point2d point)
{
  Autodesk::AutoCAD::Runtime::Interop::Check(GetImpObj()->setCenter(GETPOINT2D(point)));
}
//////////////////////////////////////////////////////////////////////////
// get the center point
Point2d BioStream::Valve::get_Center()
{
	AcGePoint2d pt;
	Autodesk::AutoCAD::Runtime::Interop::Check(GetImpObj()->getCenter(pt));
    Autodesk::AutoCAD::Geometry::Point2d ret;
    GETPOINT2D(ret) = pt;
    return ret;
}

//////////////////////////////////////////////////////////////////////////
// set the index
void BioStream::Valve::set_Index(int index)
{
  Autodesk::AutoCAD::Runtime::Interop::Check(GetImpObj()->setIndex(index));
}
//////////////////////////////////////////////////////////////////////////
// get the index
int BioStream::Valve::get_Index()
{
	Adesk::Int16 index;
	Autodesk::AutoCAD::Runtime::Interop::Check(GetImpObj()->getIndex(index));
    return (int)index;
}