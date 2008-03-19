#pragma once
#include <tchar.h>
using namespace System;
using namespace Autodesk::AutoCAD::Geometry;
using namespace Autodesk::AutoCAD::DatabaseServices;

namespace BioStream {
    
        [Autodesk::AutoCAD::Runtime::Wrapper("DbPunch")]
		public __gc class Punch :  public Autodesk::AutoCAD::DatabaseServices::Polyline
        {

        public:
            Punch();

        public private:
            Punch(System::IntPtr unmanagedPointer, bool autoDelete);
            inline DbPunch*  GetImpObj()
            {
                return static_cast<DbPunch*>(UnmanagedObject.ToPointer());
            }

        public:
        __property void set_Center(Point2d point);
        __property Point2d get_Center();

        };
    

}