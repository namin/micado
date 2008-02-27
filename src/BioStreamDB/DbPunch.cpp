// (C) Copyright 2002-2005 by Autodesk, Inc. 
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
//
// Use, duplication, or disclosure by the U.S. Government is subject to 
// restrictions set forth in FAR 52.227-19 (Commercial Computer
// Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
// (Rights in Technical Data and Computer Software), as applicable.
//

//-----------------------------------------------------------------------------
//----- DbPunch.cpp : Implementation of DbPunch
//-----------------------------------------------------------------------------
#include "StdAfx.h"
#include "DbPunch.h"

//-----------------------------------------------------------------------------
Adesk::UInt32 DbPunch::kCurrentVersionNumber =1 ;

//-----------------------------------------------------------------------------
ACRX_DXF_DEFINE_MEMBERS (
	DbPunch, AcDbPolyline,
	AcDb::kDHL_CURRENT, AcDb::kMReleaseCurrent, 
	AcDbProxyEntity::kNoOperation, DBPUNCH,
	"BIOSDBPUNCH"
	"|Product Desc:     Programmable Microfluidics Valve"
	"|Company:          MIT CSAIL CAG"
	"|WEB Address:      http://csail.mit.edu"
)

//-----------------------------------------------------------------------------
DbPunch::DbPunch () : AcDbPolyline () {
}

DbPunch::~DbPunch () {
}

//-----------------------------------------------------------------------------
//----- Biostream-specific protocols
Acad::ErrorStatus DbPunch::setCenter(const AcGePoint2d& center) {
	assertWriteEnabled();
	m_center.set(center.x, center.y);
	return Acad::eOk;
}

Acad::ErrorStatus DbPunch::getCenter(AcGePoint2d& center) const {
	assertReadEnabled();
	center.set(m_center.x, m_center.y);
	return Acad::eOk;
}

//-----------------------------------------------------------------------------
//----- AcDbObject protocols
//- Dwg Filing protocol
Acad::ErrorStatus DbPunch::dwgOutFields (AcDbDwgFiler *pFiler) const {
	assertReadEnabled () ;
	//----- Save parent class information first.
	Acad::ErrorStatus es =AcDbPolyline::dwgOutFields (pFiler) ;
	if ( es != Acad::eOk )
		return (es) ;
	//----- Object version number needs to be saved first
	if ( (es =pFiler->writeUInt32 (DbPunch::kCurrentVersionNumber)) != Acad::eOk )
		return (es) ;
	//----- Output params
	//.....
	if ( (es =pFiler->writePoint2d(DbPunch::m_center)) != Acad::eOk )
		return (es);

	return (pFiler->filerStatus ()) ;
}

Acad::ErrorStatus DbPunch::dwgInFields (AcDbDwgFiler *pFiler) {
	assertWriteEnabled () ;
	//----- Read parent class information first.
	Acad::ErrorStatus es =AcDbPolyline::dwgInFields (pFiler) ;
	if ( es != Acad::eOk )
		return (es) ;
	//----- Object version number needs to be read first
	Adesk::UInt32 version =0 ;
	if ( (es =pFiler->readUInt32 (&version)) != Acad::eOk )
		return (es) ;
	if ( version > DbPunch::kCurrentVersionNumber )
		return (Acad::eMakeMeProxy) ;
	//- Uncomment the 2 following lines if your current object implementation cannot
	//- support previous version of that object.
	//if ( version < DbPunch::kCurrentVersionNumber )
	//	return (Acad::eMakeMeProxy) ;
	//----- Read params
	//.....
	if ( (es =pFiler->readPoint2d(&(DbPunch::m_center))) != Acad::eOk )
		return (es);

	return (pFiler->filerStatus ()) ;
}

//- Dxf Filing protocol
Acad::ErrorStatus DbPunch::dxfOutFields (AcDbDxfFiler *pFiler) const {
	assertReadEnabled () ;
	//----- Save parent class information first.
	Acad::ErrorStatus es =AcDbPolyline::dxfOutFields (pFiler) ;
	if ( es != Acad::eOk )
		return (es) ;
	es =pFiler->writeItem (AcDb::kDxfSubclass, _RXST("DbPunch")) ;
	if ( es != Acad::eOk )
		return (es) ;
	//----- Object version number needs to be saved first
	if ( (es =pFiler->writeUInt32 (kDxfInt32, DbPunch::kCurrentVersionNumber)) != Acad::eOk )
		return (es) ;
	//----- Output params
	//.....
	if ( (es =pFiler->writePoint2d(kDxfXCoord, DbPunch::m_center)) != Acad::eOk )
		return (es);

	return (pFiler->filerStatus ()) ;
}

Acad::ErrorStatus DbPunch::dxfInFields (AcDbDxfFiler *pFiler) {
	assertWriteEnabled () ;
	//----- Read parent class information first.
	Acad::ErrorStatus es =AcDbPolyline::dxfInFields (pFiler) ;
	if ( es != Acad::eOk || !pFiler->atSubclassData (_RXST("DbPunch")) )
		return (pFiler->filerStatus ()) ;
	//----- Object version number needs to be read first
	struct resbuf rb ;
	pFiler->readItem (&rb) ;
	if ( rb.restype != AcDb::kDxfInt32 ) {
		pFiler->pushBackItem () ;
		pFiler->setError (Acad::eInvalidDxfCode, _RXST("\nError: expected group code %d (version #)"), AcDb::kDxfInt32) ;
		return (pFiler->filerStatus ()) ;
	}
	Adesk::UInt32 version =(Adesk::UInt32)rb.resval.rlong ;
	if ( version > DbPunch::kCurrentVersionNumber )
		return (Acad::eMakeMeProxy) ;
	//- Uncomment the 2 following lines if your current object implementation cannot
	//- support previous version of that object.
	//if ( version < DbPunch::kCurrentVersionNumber )
	//	return (Acad::eMakeMeProxy) ;
	//----- Read params in non order dependant manner
	while ( es == Acad::eOk && (es =pFiler->readResBuf (&rb)) == Acad::eOk ) {
		switch ( rb.restype ) {
			//----- Read params by looking at their DXF code (example below)
			//case AcDb::kDxfXCoord:
			//	if ( version == 1 )
			//		cen3d =asPnt3d (rb.resval.rpoint) ;
			//	else 
			//		cen2d =asPnt2d (rb.resval.rpoint) ;
			//	break ;
			//.....
			case AcDb::kDxfXCoord:
				DbPunch::m_center = asPnt2d (rb.resval.rpoint);
				break;

			default:
				//----- An unrecognized group. Push it back so that the subclass can read it again.
				pFiler->pushBackItem () ;
				es =Acad::eEndOfFile ;
				break ;
		}
	}
	//----- At this point the es variable must contain eEndOfFile
	//----- - either from readResBuf() or from pushback. If not,
	//----- it indicates that an error happened and we should
	//----- return immediately.
	if ( es != Acad::eEndOfFile )
		return (Acad::eInvalidResBuf) ;

	return (pFiler->filerStatus ()) ;
}

//- Automation support
Acad::ErrorStatus DbPunch::getClassID (CLSID *pClsid) const {
	assertReadEnabled () ;
	//::CLSIDFromProgID (L"BioStreamDB.EntityComWrapperInt", pClsid) ;
	//return (Acad::eOk) ;
	return (AcDbPolyline::getClassID (pClsid)) ;
}

//-----------------------------------------------------------------------------
//----- AcDbEntity protocols
Adesk::Boolean DbPunch::worldDraw (AcGiWorldDraw *mode) {
	assertReadEnabled () ;
	return (AcDbPolyline::worldDraw (mode)) ;
}


//-----------------------------------------------------------------------------
//----- AcDbCurve protocols
//- Curve property tests.
Adesk::Boolean DbPunch::isClosed () const {
	assertReadEnabled () ;
	return (AcDbPolyline::isClosed ()) ;
}

Adesk::Boolean DbPunch::isPeriodic () const {
	assertReadEnabled () ;
	return (AcDbPolyline::isPeriodic ()) ;
}
      
//- Get planar and start/end geometric properties.
Acad::ErrorStatus DbPunch::getStartParam (double &param) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getStartParam (param)) ;
}

Acad::ErrorStatus DbPunch::getEndParam (double &param) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getEndParam (param)) ;
}

Acad::ErrorStatus DbPunch::getStartPoint (AcGePoint3d &point) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getStartPoint (point)) ;
}

Acad::ErrorStatus DbPunch::getEndPoint (AcGePoint3d &point) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getEndPoint (point)) ;
}

//- Conversions to/from parametric/world/distance.
Acad::ErrorStatus DbPunch::getPointAtParam (double param, AcGePoint3d &point) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getPointAtParam (param, point)) ;
}

Acad::ErrorStatus DbPunch::getParamAtPoint (const AcGePoint3d &point, double &param) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getParamAtPoint (point, param)) ;
}

Acad::ErrorStatus DbPunch::getDistAtParam (double param, double &dist) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getDistAtParam (param, dist)) ;
}

Acad::ErrorStatus DbPunch::getParamAtDist (double dist, double &param) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getParamAtDist (dist, param)) ;
}

Acad::ErrorStatus DbPunch::getDistAtPoint (const AcGePoint3d &point , double &dist) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getDistAtPoint (point, dist)) ;
}

Acad::ErrorStatus DbPunch::getPointAtDist (double dist, AcGePoint3d &point) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getPointAtDist (dist, point)) ;
}

//- Derivative information.
Acad::ErrorStatus DbPunch::getFirstDeriv (double param, AcGeVector3d &firstDeriv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getFirstDeriv (param, firstDeriv)) ;
}

Acad::ErrorStatus DbPunch::getFirstDeriv  (const AcGePoint3d &point, AcGeVector3d &firstDeriv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getFirstDeriv (point, firstDeriv)) ;
}

Acad::ErrorStatus DbPunch::getSecondDeriv (double param, AcGeVector3d &secDeriv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSecondDeriv (param, secDeriv)) ;
}

Acad::ErrorStatus DbPunch::getSecondDeriv (const AcGePoint3d &point, AcGeVector3d &secDeriv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSecondDeriv (point, secDeriv)) ;
}

//- Closest point on curve.
Acad::ErrorStatus DbPunch::getClosestPointTo (const AcGePoint3d &givenPnt, AcGePoint3d &pointOnCurve, Adesk::Boolean extend /*=Adesk::kFalse*/) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getClosestPointTo (givenPnt, pointOnCurve, extend)) ;
}

Acad::ErrorStatus DbPunch::getClosestPointTo (const AcGePoint3d &givenPnt, const AcGeVector3d &direction, AcGePoint3d &pointOnCurve, Adesk::Boolean extend /*=Adesk::kFalse*/) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getClosestPointTo (givenPnt, direction, pointOnCurve, extend)) ;
}

//- Get a projected copy of the curve.
Acad::ErrorStatus DbPunch::getOrthoProjectedCurve (const AcGePlane &plane, AcDbCurve *&projCrv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getOrthoProjectedCurve (plane, projCrv)) ;
}

Acad::ErrorStatus DbPunch::getProjectedCurve (const AcGePlane &plane, const AcGeVector3d &projDir, AcDbCurve *&projCrv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getProjectedCurve (plane, projDir, projCrv)) ;
}

//- Get offset, spline and split copies of the curve.
Acad::ErrorStatus DbPunch::getOffsetCurves (double offsetDist, AcDbVoidPtrArray &offsetCurves) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getOffsetCurves (offsetDist, offsetCurves)) ;
}

Acad::ErrorStatus DbPunch::getOffsetCurvesGivenPlaneNormal (const AcGeVector3d &normal, double offsetDist, AcDbVoidPtrArray &offsetCurves) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getOffsetCurvesGivenPlaneNormal (normal, offsetDist, offsetCurves)) ;
}

Acad::ErrorStatus DbPunch::getSpline (AcDbSpline *&spline) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSpline (spline)) ;
}

Acad::ErrorStatus DbPunch::getSplitCurves (const AcGeDoubleArray &params, AcDbVoidPtrArray &curveSegments) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSplitCurves (params, curveSegments)) ;
}

Acad::ErrorStatus DbPunch::getSplitCurves (const AcGePoint3dArray &points, AcDbVoidPtrArray &curveSegments) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSplitCurves (points, curveSegments)) ;
}

//- Extend the curve.
Acad::ErrorStatus DbPunch::extend (double newParam) {
	assertReadEnabled () ;
	return (AcDbPolyline::extend (newParam)) ;
}

Acad::ErrorStatus DbPunch::extend (Adesk::Boolean extendStart, const AcGePoint3d &toPoint) {
	assertReadEnabled () ;
	return (AcDbPolyline::extend (extendStart, toPoint)) ;
}

//- Area calculation.
Acad::ErrorStatus DbPunch::getArea (double &area) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getArea (area)) ;
}


// -----------------------------------------------------------------------------
Acad::ErrorStatus DbPunch::transformBy(const AcGeMatrix3d & xform)
{
	Acad::ErrorStatus retCode =AcDbPolyline::transformBy (xform) ;
	AcGeVector3d vecZ = AcGeVector3d(0, 0, 1);
	double elev = 0.0;
	const AcGeMatrix2d & xform2d = xform.convertToLocal(vecZ, elev);
	m_center.transformBy(xform2d);
	return (retCode) ;
}
