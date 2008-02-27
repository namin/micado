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
BioStream::Punch::Punch() 
:Autodesk::AutoCAD::DatabaseServices::Polyline(new DbPunch(), true)
{
	//acutPrintf(_T("\n*********************Constructor"));
}

//////////////////////////////////////////////////////////////////////////
BioStream::Punch::Punch(System::IntPtr unmanagedPointer, bool autoDelete)
: Autodesk::AutoCAD::DatabaseServices::Polyline(unmanagedPointer,autoDelete)
{
}

//////////////////////////////////////////////////////////////////////////
// set the centre of the poly
void BioStream::Punch::set_Center(Point2d point)
{
  Autodesk::AutoCAD::Runtime::Interop::Check(GetImpObj()->setCenter(GETPOINT2D(point)));
}
//////////////////////////////////////////////////////////////////////////
// get the center point
Point2d BioStream::Punch::get_Center()
{
	AcGePoint2d pt;
	Autodesk::AutoCAD::Runtime::Interop::Check(GetImpObj()->getCenter(pt));
    Autodesk::AutoCAD::Geometry::Point2d ret;
    GETPOINT2D(ret) = pt;
    return ret;
}
