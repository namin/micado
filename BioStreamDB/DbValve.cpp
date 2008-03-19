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
//----- DbValve.cpp : Implementation of DbValve
//-----------------------------------------------------------------------------
#include "StdAfx.h"
#include "DbValve.h"

//-----------------------------------------------------------------------------
Adesk::UInt32 DbValve::kCurrentVersionNumber =2 ;

//-----------------------------------------------------------------------------
ACRX_DXF_DEFINE_MEMBERS (
	DbValve, AcDbPolyline,
	AcDb::kDHL_CURRENT, AcDb::kMReleaseCurrent, 
	AcDbProxyEntity::kNoOperation, DBVALVE,
	"BIOSDBVALVE"
	"|Product Desc:     Programmable Microfluidics Valve"
	"|Company:          MIT CSAIL CAG"
	"|WEB Address:      http://csail.mit.edu"
)

//-----------------------------------------------------------------------------
DbValve::DbValve () : AcDbPolyline () {
	m_index = -1;
}

DbValve::~DbValve () {
}

//-----------------------------------------------------------------------------
//----- Biostream-specific protocols
Acad::ErrorStatus DbValve::setCenter(const AcGePoint2d& center) {
	assertWriteEnabled();
	m_center.set(center.x, center.y);
	return Acad::eOk;
}

Acad::ErrorStatus DbValve::getCenter(AcGePoint2d& center) const {
	assertReadEnabled();
	center.set(m_center.x, m_center.y);
	return Acad::eOk;
}

Acad::ErrorStatus DbValve::setIndex(const Adesk::Int16& index) {
	assertWriteEnabled();
	m_index = index;
	return Acad::eOk;
}

Acad::ErrorStatus DbValve::getIndex(Adesk::Int16& index) const {
	assertReadEnabled();
	index = m_index;
	return Acad::eOk;
}

//-----------------------------------------------------------------------------
//----- AcDbObject protocols
//- Dwg Filing protocol
Acad::ErrorStatus DbValve::dwgOutFields (AcDbDwgFiler *pFiler) const {
	assertReadEnabled () ;
	//----- Save parent class information first.
	Acad::ErrorStatus es =AcDbPolyline::dwgOutFields (pFiler) ;
	if ( es != Acad::eOk )
		return (es) ;
	//----- Object version number needs to be saved first
	if ( (es =pFiler->writeUInt32 (DbValve::kCurrentVersionNumber)) != Acad::eOk )
		return (es) ;
	//----- Output params
	//.....
	if ( (es =pFiler->writePoint2d(DbValve::m_center)) != Acad::eOk )
		return (es);
	if ( (es =pFiler->writeInt16(DbValve::m_index)) != Acad::eOk )
		return (es);

	return (pFiler->filerStatus ()) ;
}

Acad::ErrorStatus DbValve::dwgInFields (AcDbDwgFiler *pFiler) {
	assertWriteEnabled () ;
	//----- Read parent class information first.
	Acad::ErrorStatus es =AcDbPolyline::dwgInFields (pFiler) ;
	if ( es != Acad::eOk )
		return (es) ;
	//----- Object version number needs to be read first
	Adesk::UInt32 version =0 ;
	if ( (es =pFiler->readUInt32 (&version)) != Acad::eOk )
		return (es) ;
	if ( version > DbValve::kCurrentVersionNumber )
		return (Acad::eMakeMeProxy) ;
	//- Uncomment the 2 following lines if your current object implementation cannot
	//- support previous version of that object.
	//if ( version < DbValve::kCurrentVersionNumber )
	//	return (Acad::eMakeMeProxy) ;
	//----- Read params
	//.....
	if ( (es =pFiler->readPoint2d(&(DbValve::m_center))) != Acad::eOk )
		return (es);
	if ( version >= 2 ) {
		if ( (es =pFiler->readInt16(&(DbValve::m_index))) != Acad::eOk )
			return (es);
	}
	return (pFiler->filerStatus ()) ;
}

//- Dxf Filing protocol
Acad::ErrorStatus DbValve::dxfOutFields (AcDbDxfFiler *pFiler) const {
	assertReadEnabled () ;
	//----- Save parent class information first.
	Acad::ErrorStatus es =AcDbPolyline::dxfOutFields (pFiler) ;
	if ( es != Acad::eOk )
		return (es) ;
	es =pFiler->writeItem (AcDb::kDxfSubclass, _RXST("DbValve")) ;
	if ( es != Acad::eOk )
		return (es) ;
	//----- Object version number needs to be saved first
	if ( (es =pFiler->writeUInt32 (kDxfInt32, DbValve::kCurrentVersionNumber)) != Acad::eOk )
		return (es) ;
	//----- Output params
	//.....
	if ( (es =pFiler->writePoint2d(kDxfXCoord, DbValve::m_center)) != Acad::eOk )
		return (es);
	if ( (es =pFiler->writeInt16(kDxfInt16, DbValve::m_index)) != Acad::eOk )
		return (es);

	return (pFiler->filerStatus ()) ;
}

Acad::ErrorStatus DbValve::dxfInFields (AcDbDxfFiler *pFiler) {
	assertWriteEnabled () ;
	//----- Read parent class information first.
	Acad::ErrorStatus es =AcDbPolyline::dxfInFields (pFiler) ;
	if ( es != Acad::eOk || !pFiler->atSubclassData (_RXST("DbValve")) )
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
	if ( version > DbValve::kCurrentVersionNumber )
		return (Acad::eMakeMeProxy) ;
	//- Uncomment the 2 following lines if your current object implementation cannot
	//- support previous version of that object.
	//if ( version < DbValve::kCurrentVersionNumber )
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
				DbValve::m_center = asPnt2d (rb.resval.rpoint);
				break;
			case AcDb::kDxfInt16:
				DbValve::m_index = (Adesk::Int16)rb.resval.rint ;
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

//-----------------------------------------------------------------------------
//----- AcDbEntity protocols
Adesk::Boolean DbValve::worldDraw (AcGiWorldDraw *mode) {
	assertReadEnabled () ;
	return (AcDbPolyline::worldDraw (mode)) ;
}


//-----------------------------------------------------------------------------
//----- AcDbCurve protocols
//- Curve property tests.
Adesk::Boolean DbValve::isClosed () const {
	assertReadEnabled () ;
	return (AcDbPolyline::isClosed ()) ;
}

Adesk::Boolean DbValve::isPeriodic () const {
	assertReadEnabled () ;
	return (AcDbPolyline::isPeriodic ()) ;
}
      
//- Get planar and start/end geometric properties.
Acad::ErrorStatus DbValve::getStartParam (double &param) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getStartParam (param)) ;
}

Acad::ErrorStatus DbValve::getEndParam (double &param) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getEndParam (param)) ;
}

Acad::ErrorStatus DbValve::getStartPoint (AcGePoint3d &point) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getStartPoint (point)) ;
}

Acad::ErrorStatus DbValve::getEndPoint (AcGePoint3d &point) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getEndPoint (point)) ;
}

//- Conversions to/from parametric/world/distance.
Acad::ErrorStatus DbValve::getPointAtParam (double param, AcGePoint3d &point) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getPointAtParam (param, point)) ;
}

Acad::ErrorStatus DbValve::getParamAtPoint (const AcGePoint3d &point, double &param) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getParamAtPoint (point, param)) ;
}

Acad::ErrorStatus DbValve::getDistAtParam (double param, double &dist) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getDistAtParam (param, dist)) ;
}

Acad::ErrorStatus DbValve::getParamAtDist (double dist, double &param) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getParamAtDist (dist, param)) ;
}

Acad::ErrorStatus DbValve::getDistAtPoint (const AcGePoint3d &point , double &dist) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getDistAtPoint (point, dist)) ;
}

Acad::ErrorStatus DbValve::getPointAtDist (double dist, AcGePoint3d &point) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getPointAtDist (dist, point)) ;
}

//- Derivative information.
Acad::ErrorStatus DbValve::getFirstDeriv (double param, AcGeVector3d &firstDeriv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getFirstDeriv (param, firstDeriv)) ;
}

Acad::ErrorStatus DbValve::getFirstDeriv  (const AcGePoint3d &point, AcGeVector3d &firstDeriv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getFirstDeriv (point, firstDeriv)) ;
}

Acad::ErrorStatus DbValve::getSecondDeriv (double param, AcGeVector3d &secDeriv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSecondDeriv (param, secDeriv)) ;
}

Acad::ErrorStatus DbValve::getSecondDeriv (const AcGePoint3d &point, AcGeVector3d &secDeriv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSecondDeriv (point, secDeriv)) ;
}

//- Closest point on curve.
Acad::ErrorStatus DbValve::getClosestPointTo (const AcGePoint3d &givenPnt, AcGePoint3d &pointOnCurve, Adesk::Boolean extend /*=Adesk::kFalse*/) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getClosestPointTo (givenPnt, pointOnCurve, extend)) ;
}

Acad::ErrorStatus DbValve::getClosestPointTo (const AcGePoint3d &givenPnt, const AcGeVector3d &direction, AcGePoint3d &pointOnCurve, Adesk::Boolean extend /*=Adesk::kFalse*/) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getClosestPointTo (givenPnt, direction, pointOnCurve, extend)) ;
}

//- Get a projected copy of the curve.
Acad::ErrorStatus DbValve::getOrthoProjectedCurve (const AcGePlane &plane, AcDbCurve *&projCrv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getOrthoProjectedCurve (plane, projCrv)) ;
}

Acad::ErrorStatus DbValve::getProjectedCurve (const AcGePlane &plane, const AcGeVector3d &projDir, AcDbCurve *&projCrv) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getProjectedCurve (plane, projDir, projCrv)) ;
}

//- Get offset, spline and split copies of the curve.
Acad::ErrorStatus DbValve::getOffsetCurves (double offsetDist, AcDbVoidPtrArray &offsetCurves) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getOffsetCurves (offsetDist, offsetCurves)) ;
}

Acad::ErrorStatus DbValve::getOffsetCurvesGivenPlaneNormal (const AcGeVector3d &normal, double offsetDist, AcDbVoidPtrArray &offsetCurves) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getOffsetCurvesGivenPlaneNormal (normal, offsetDist, offsetCurves)) ;
}

Acad::ErrorStatus DbValve::getSpline (AcDbSpline *&spline) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSpline (spline)) ;
}

Acad::ErrorStatus DbValve::getSplitCurves (const AcGeDoubleArray &params, AcDbVoidPtrArray &curveSegments) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSplitCurves (params, curveSegments)) ;
}

Acad::ErrorStatus DbValve::getSplitCurves (const AcGePoint3dArray &points, AcDbVoidPtrArray &curveSegments) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getSplitCurves (points, curveSegments)) ;
}

//- Extend the curve.
Acad::ErrorStatus DbValve::extend (double newParam) {
	assertReadEnabled () ;
	return (AcDbPolyline::extend (newParam)) ;
}

Acad::ErrorStatus DbValve::extend (Adesk::Boolean extendStart, const AcGePoint3d &toPoint) {
	assertReadEnabled () ;
	return (AcDbPolyline::extend (extendStart, toPoint)) ;
}

//- Area calculation.
Acad::ErrorStatus DbValve::getArea (double &area) const {
	assertReadEnabled () ;
	return (AcDbPolyline::getArea (area)) ;
}


// -----------------------------------------------------------------------------
Acad::ErrorStatus DbValve::transformBy(const AcGeMatrix3d & xform)
{
	Acad::ErrorStatus retCode =AcDbPolyline::transformBy (xform) ;
	AcGeVector3d vecZ = AcGeVector3d(0, 0, 1);
	double elev = 0.0;
	const AcGeMatrix2d & xform2d = xform.convertToLocal(vecZ, elev);
	m_center.transformBy(xform2d);
	return (retCode) ;
}
