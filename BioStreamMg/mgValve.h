#pragma once
#include <tchar.h>
using namespace System;
using namespace Autodesk::AutoCAD::Geometry;
using namespace Autodesk::AutoCAD::DatabaseServices;

namespace BioStream {
    
        [Autodesk::AutoCAD::Runtime::Wrapper("DbValve")]
		public __gc class Valve :  public Autodesk::AutoCAD::DatabaseServices::Polyline
        {

        public:
            Valve();

        public private:
            Valve(System::IntPtr unmanagedPointer, bool autoDelete);
            inline DbValve*  GetImpObj()
            {
                return static_cast<DbValve*>(UnmanagedObject.ToPointer());
            }

        public:
        __property void set_Center(Point2d point);
        __property Point2d get_Center();

		__property void set_Index(int index);
        __property int get_Index();
        };
    

}