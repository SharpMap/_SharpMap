using System;
using OSGeo.GDAL;
using OSGeo.OGR;
using SpatialReference=OSGeo.OSR.SpatialReference;

namespace SharpMap.Extensions.Data.Providers
{
    public static class OgrHelper
    {
        public static string Amersfoort()
        {
            string proj4 =
                "+proj=sterea +lat_0=52.15616055555555 +lon_0=5.38763888888889 +k=0.9999079 +x_0=155000 +y_0=463000 +ellps=bessel +units=m +no_defs";


            using(var osrs= new SpatialReference("+proj=sterea +lat_0=52.15616055555555 +lon_0=5.38763888888889 +k=0.9999079 +x_0=155000 +y_0=463000 +ellps=bessel +units=m +no_defs"))
            {
                Ogr.RegisterAll();
                string stringOutput;
                osrs.ExportToProj4(out stringOutput);
                return stringOutput;
            }
        }

        public static string  EsriToProj4(string esriProjectionPath)
        {
            OSGeo.OSR.SpatialReference oSRS= new SpatialReference("");
           if (oSRS.SetFromUserInput(esriProjectionPath)!=Ogr.OGRERR_NONE)
           {
               throw new ApplicationException(string.Format("Error occured translating {0}",esriProjectionPath));
           }

            string proj4OutputString;
            if( oSRS.ExportToProj4(out proj4OutputString)==Ogr.OGRERR_NONE)
            {
                return proj4OutputString;
            }
            else
            {
                throw new ApplicationException("Export to proj4 failed");
            }

/*
            if (nArgc != 2)
            {
                printf("Usage: wkt2proj4 [srtext def or srtext file]\n");
                exit(1);
            }

            if (oSRS.SetFromUserInput(papszArgv[1]) != OGRERR_NONE)
            {
                CPLError(CE_Failure, CPLE_AppDefined,
                        "Error occured translating %s.\n", papszArgv[1]);
            }

            oSRS.morphFromESRI();

            char* pszProj4 = NULL;

            if (oSRS.exportToProj4(&pszProj4) == OGRERR_NONE)
            {
                printf("%s\n", pszProj4);
            }
            else
            {
                fprintf(stderr, "exportToProj4() failed.\n");
            }

*/
        }

    
     

    }
}