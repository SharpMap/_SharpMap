//using System.Drawing;
//using System.Windows.Forms;

//namespace SharpMap.UI.Tools
//{
//    internal class CWScaleBar
//    {
//        private bool m_bWindowOnly;
//        private bool m_bBorderVisible;
//        private Color m_clrBackColor;
//        private Color m_clrBorderColor;
//        private Color m_clrForeColor;
//        private int m_nBorderWidth;
//        private Color m_clrBarColor1;
//        private Color m_clrBarColor2;
//        private MapUnits m_nMapUnit;
//        private ScaleBarUnits m_nBarUnit;
//        private double m_fScale;
//        private double m_fMapWidth;
//        private int m_nPageWidth;
//        private double m_fLon1;
//        private double m_fLon2;
//        private double m_fLat;
//        private int m_nNumTics;
//        private bool m_bBarOutline;
//        private Color m_clrBarOutline;
//        private sbScaleText m_sbScaleText;
//        private int m_nBarWidth;
//        private ScaleBarStyle m_barStyle;
//        private bool m_bTransparentBG;
//        private object m_bFormatNumber;
//        private int m_nMarginLeft;
//        private int m_nMarginRight;

//        const int PowerRangeMin=-5;
//const int PowerRangeMax=10;
//const int nNiceNumber=4;
//const int defaultBarWidth=5;
////const double[] NiceNumberArray=double[nNiceNumber]{1, 2, 2.5, 5};
//const int ScalePrecisionDigits=5;
//const int GapScaleText_Bar=3;
//const int GapBar_SegmentText=1;

//const int defaultMapUnit           =9001; //meter
//const int defaultScaleBarUnit      =9001; //meter
//        private const bool defaultBorderVisible = false;
//        private const bool defaultBarOutline = true;
//const int defaultBorderWidth       =1;
//const int defaultBorderStyle       =PS_SOLID;
//const Color  defaultBorderColor=0x0;
//const Color defaultForeColor   =0x0;
//const Color defaultBgColor     =0xffffff;
//const Color defaultBarOutlineCr=0x0;
//const Color defaultBarColor1   =0xff0000;
//const Color defaultBarColor2   =0xffffff;

//const double DTR = 0.01745329252; // Convert Degrees to Radians
//const double metersPerInch=0.0254; 
//const double metersPerMile=1609.347219;
//const double milesPerDegreeAtEquator=69.171;
//const double metersPerDegreeAtEquator= metersPerMile * milesPerDegreeAtEquator;

//const int defaultNumTics=4;
//const int defaultMarginX=4;
//const double fVerySmall=0.0000001;

//const int MaxNameLength=80;
//const int UNIT_NUMS=8;

//  enum MapUnits
//    {
//        ws_muCustom        = 0,
//         ws_muMeter         = 9001,
//       ws_muFootUS        = 9003,
//       ws_muYardSears  = 9012,
//       ws_muYardIndian= 9013,
//       ws_muMileUS        = 9035,
//       ws_muKilometer   = 9036,
//       ws_muDegree         = 9102
//    }
//        enum ScaleBarUnits
//    {
//        ws_suCustom= 0,
//         ws_suMeter  = 9001,
//       ws_suFootUS = 9003,
//      ws_suYardSears  = 9012,
//       ws_suYardIndian= 9013,
//       ws_suMileUS = 9035,
//       ws_suKilometer = 9036
//    } 


//                    enum sbScaleText
//    {
//         ws_stNoText   = 0,
//       ws_stUnitsOnly= 1,
//       ws_stFraction= 2
//    }
//        enum ScaleBarStyle
//    {
//       ws_bsStandard=0,
//       ws_bsMeridian =1,
//       ws_bsMeridian1=2
//    } 


//CWScaleBar()
//{
////initial value
//  m_bWindowOnly = true;

//  m_bBorderVisible =defaultBorderVisible;
//  m_clrBackColor   =defaultBgColor;
//  m_clrBorderColor =defaultBorderColor;
//  m_clrForeColor   =defaultForeColor;
//  m_nBorderStyle   =ws_psSolid;
//  m_nBorderWidth   =defaultBorderWidth;
//  m_clrBarColor1   =defaultBarColor1;
//  m_clrBarColor2   =defaultBarColor2;

////unit
//  m_nMapUnit       =MapUnits.ws_muMeter;//defaultMapUnit;
//  GetUnitInformation(m_nMapUnit, m_fMapUnitFactor, m_strMapUnitName, m_strMapUnitShortName);  //update the Map unit information
//  m_nBarUnit       =ScaleBarUnits.ws_suMeter;//defaultScaleBarUnit;
//  GetUnitInformation(m_nBarUnit, m_fBarUnitFactor, m_strBarUnitName, m_strBarUnitShortName);  //update the scalebar unit information
////scale
//  m_fScale         =0.0; //Initial scale.  
//  m_fMapWidth      =0.0;
//  m_nPageWidth     =0;
//  m_fLon1          =0.0;
//  m_fLon2          =0.0;
//  m_fLat           =0.0;
////bar
//    m_nNumTics = defaultNumTics;
//    m_bBarOutline    =defaultBarOutline;
//  m_clrBarOutline  =defaultBarOutlineCr;
//  m_nBarWidth      =defaultBarWidth;
//  m_sbScaleText    =sbScaleText.ws_stFraction;
//  m_barStyle       =ScaleBarStyle.ws_bsStandard;
////control
//  m_bTransparentBG =false;
//  m_bFormatNumber  =false;
//  // Create a default font to use with this control.
////  static System.Windows.Forms.NativeMethods.FONTDESC _fontDesc = { sizeof(System.Windows.Forms.NativeMethods.FONTDESC), OLESTR("Times New Roman"), 
////			FONTSIZE(12), FW_BOLD, ANSI_CHARSET, FALSE, FALSE, FALSE };
// // OleCreateFontIndirect( &_fontDesc, IID_IFontDisp, (void **)&m_pFont);  

//  m_nMarginLeft =defaultMarginX;  //left margin for the scale bar
//  m_nMarginRight=defaultMarginX;  //right margin for the scale bar
//}

//void AboutBox()
//{
    
//    // TODO: Add your implementation code here
//  //CSimpleDialog<IDD_ABOUTBOX> d;
//  //d.DoModal();
//  //return S_OK;
//}

//HRESULT CWScaleBar::OnDraw(ATL_DRAWINFO& di)
//{
//  RECT& rc = *(RECT*)di.prcBounds;
//  HDC hdc=di.hdcDraw;
//  DrawTheControl(hdc, rc);
//  return S_OK;
//}

////Draw the whole control on the hdc. The drawing area is rc.
//void CWScaleBar::DrawTheControl(HDC hdc, RECT rc)
//{
//  int nWidthDC =rc.right-rc.left;
//  int nHeightDC=rc.bottom-rc.top;
//  int nPixelsPerTic;
//  double SBUnitsPerTic;

//  if (!m_bTransparentBG)
//    DrawBackground(hdc, rc);
//  if (m_bBorderVisible && m_nBorderWidth>0)
//    DrawBorder(hdc, rc);

//  int nOffsetX, nOffsetY;
////Get the scale first.
//  if (m_fScale<fVerySmall)
//    return; //return if the scale is just too small
////Initialize the locale. So the we can use the latest locale setting to show the numbers.
//  m_locale.Init();

////Draw the bar.
//  CalcBarScale(nWidthDC, m_nNumTics, m_fScale, m_fBarUnitFactor, nPixelsPerTic, SBUnitsPerTic);
//  nOffsetX=(nWidthDC - m_nNumTics * nPixelsPerTic-m_nMarginLeft-m_nMarginRight)/2+m_nMarginLeft;  //left margin 
//  nOffsetY=(nHeightDC - m_nBarWidth)/2;    //vertical center                                       
//  DrawBar(hdc, nPixelsPerTic, nOffsetX, nOffsetY);
//  DrawVerbalScale(hdc, nWidthDC/2, nOffsetY - GapScaleText_Bar);
//  DrawSegmentText(hdc, nOffsetX, nOffsetY+m_nBarWidth+GapBar_SegmentText, m_nNumTics, nPixelsPerTic, SBUnitsPerTic, m_strBarUnitShortName);
//}

////Draw the scalebar on hdc. nTicLength is the length of every tics (in pixel).
////nOffsetX/nOffsetY is the left/top margin for the bar.
//void CWScaleBar::DrawBar(HDC hdc, int nTicLength, int nOffsetX, int nOffsetY)
//{
//  COLORREF cr1, cr2;
//  COLORREF crOutline;
//  OleTranslateColor(m_clrBarColor1, NULL, &cr1);
//  OleTranslateColor(m_clrBarColor2, NULL, &cr2);
//  OleTranslateColor(m_clrBarOutline, NULL, &crOutline);
//  DrawBarWithStyle(hdc, nOffsetX, nOffsetY, m_nNumTics, nTicLength, m_nBarWidth, cr1, cr2, m_bBarOutline, crOutline, m_barStyle);
//}

////Draw the background for the control.
//void CWScaleBar::DrawBackground(HDC hdc, RECT rc)
//{
//  COLORREF crPen, crFill;
//  int nPenStyle;
//  OleTranslateColor(m_clrBackColor, NULL, &crFill);
//  OleTranslateColor(m_clrBorderColor, NULL, &crPen);

//  nPenStyle=PS_NULL;

////increase the right and bottom 
//  rc.right=rc.right+1;
//  rc.bottom=rc.bottom+1;
//  FillRectangle(hdc, rc, BS_SOLID, crFill, nPenStyle, crPen, m_nBorderWidth);
//}

////Draw the border for the control.
//void CWScaleBar::DrawBorder(HDC hdc, RECT rc)
//{
//  COLORREF crPen;
//  int nPenStyle;
//  OleTranslateColor(m_clrBorderColor, NULL, &crPen);

//  nPenStyle=m_nBorderStyle;
////resize the rectangle to draw the border 
//  rc.left  =rc.left + m_nBorderWidth/2;
//  rc.top   =rc.top  + m_nBorderWidth/2;
//  rc.right =rc.right- (m_nBorderWidth-1)/2;
//  rc.bottom=rc.bottom -(m_nBorderWidth-1)/2;
//  FillRectangle(hdc, rc, BS_NULL, 0, nPenStyle, crPen, m_nBorderWidth);
//}

////Draw the verbal text above the scale bar.
////(x, y) is the reference point of the text. x is the center of the bar, y is the top of the bar.
//void CWScaleBar::DrawVerbalScale(HDC hdc, int x, int y)
//{
//  char buf[MAX_LEN];
//  memset(buf, 0, MAX_LEN);
////Get the scale text.
//  ScaleBarText(m_fScale, m_sbScaleText, buf, MAX_LEN);
////Draw the text.
//  DrawTextWithAlign(hdc, buf, x, y, TA_BOTTOM | TA_CENTER);
//}

////(x, y) is the reference point for the segment text. x is the beginning point for the bar. y is the bottom of the bar.
//void CWScaleBar::DrawSegmentText(HDC hdc, int x, int y, int Tics, int TicWidth, double SBUnitsPerTic, char *str_unit)
//{
//  int i;
//  int precision;
//  ostrstream s;
//  double value;
//  int Align;
//  char buf[MAX_LEN];

//  if (m_sbScaleText==ws_stUnitsOnly)
//    DrawTextWithAlign(hdc, "0", x, y, TA_TOP | TA_LEFT);
//  else 
//    DrawTextWithAlign(hdc, str_unit, x, y, TA_TOP | TA_LEFT);

////Set the output format.
//  precision=PresitionOfSegmentText(SBUnitsPerTic);
//  s.precision(precision);
//  s.setf(ios::left, ios::adjustfield);
//  s.setf(ios::fixed, ios::floatfield);

//  for (i=1; i<=Tics; i++) {
//    value=SBUnitsPerTic * i;
//    s << value << ends;

///*	if (i<Tics)
//      Align=TA_TOP | TA_CENTER;
//    else
//      Align=TA_TOP | TA_RIGHT;*/  //the last one is right align
//    Align=TA_TOP | TA_CENTER;	
//    if (m_bFormatNumber) { //format the number
//      memset(buf, 0, MAX_LEN);
//      m_locale.FormatNumberStr(s.str(), buf, MAX_LEN-1);
//      DrawTextWithAlign(hdc, buf, x + TicWidth * i, y, Align); 
//    }
//    else
//      DrawTextWithAlign(hdc, s.str(), x + TicWidth * i, y, Align); 

////empty the string for the next output.
//    s.rdbuf()->freeze(false);
//    s.seekp(ios::beg);
//  }
//}

////Draw the string (str) on hdc.
////(x, y) is the reference point for the string.
////nWidth is the width for the string.
////TextAlign is the align style for the string.
//void CWScaleBar::DrawTextWithAlign(HDC hdc, char *str, int x, int y, UINT TextAlign)
//{
//  HFONT oldFont, newFont;
//  COLORREF newFontCr, oldFontCr;
//  int oldBkMode;
//  UINT oldAlign;
//  int L=strlen(str);

////Set the color 
//  OleTranslateColor(m_clrForeColor, NULL, &newFontCr);
//  oldFontCr=SetTextColor(hdc, newFontCr);
//  oldAlign=SetTextAlign(hdc, TextAlign);
//  oldBkMode=SetBkMode(hdc, TRANSPARENT);

//// Set the font
//  CComQIPtr<IFont, &IID_IFont> pFont(m_pFont);
//  if (pFont != NULL) {
//    pFont->get_hFont(&newFont);
//    pFont->AddRefHfont(newFont);
//    oldFont = (HFONT) SelectObject(hdc, newFont);
//  }

////Output the text.
//  TextOut(hdc, x, y, str, L);

////Restore the old settings.
//  SetTextColor(hdc, oldFontCr);
//  SetBkMode(hdc, oldBkMode);
//  SetTextAlign(hdc, oldAlign);
//  SelectObject(hdc, oldFont);
////release resource
//  pFont->ReleaseHfont(newFont);
//}

////Calculate the scale and store it in m_fScale.
////It should be called to calculate the real map scale everytime the user change mapunit or set the scale
//void CWScaleBar::CalcScale()
//{
//  double fScale=0.0;
//  if (m_nMapUnit==ws_muDegree) //LatLong
//    fScale=CalcRFScaleD(m_fLon1, m_fLon2, m_fLat, m_nPageWidth);
//  else
//    fScale=CalcRFScale(m_fMapWidth, m_nPageWidth, m_fMapUnitFactor);
//  m_fScale=fScale;
//}

//STDMETHODIMP CWScaleBar::get_BarColor1(OLE_COLOR *pVal)
//{
//  *pVal=m_clrBarColor1;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_BarColor1(OLE_COLOR newVal)
//{
//  m_clrBarColor1=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_BarColor2(OLE_COLOR *pVal)
//{
//  *pVal=m_clrBarColor2;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_BarColor2(OLE_COLOR newVal)
//{
//  m_clrBarColor2=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_BarWidth(int *pVal)
//{
//  *pVal=m_nBarWidth;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_BarWidth(int newVal)
//{
//  m_nBarWidth=newVal;
//  if (m_nBarWidth<1)
//    m_nBarWidth=1;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_MapUnit(MapUnits *pVal)
//{
//  *pVal=m_nMapUnit;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_MapUnit(MapUnits newVal)
//{
//  m_nMapUnit=newVal;
//  GetUnitInformation(m_nMapUnit, m_fMapUnitFactor, m_strMapUnitName, m_strMapUnitShortName);  //update the Map unit information
//  CalcScale();
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_BarUnit(ScaleBarUnits *pVal)
//{
//  *pVal=m_nBarUnit;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_BarUnit(ScaleBarUnits newVal)
//{
//  m_nBarUnit=newVal;
//  GetUnitInformation(m_nBarUnit, m_fBarUnitFactor, m_strBarUnitName, m_strBarUnitShortName);  //update the scalebar unit information
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_BarOutline(VARIANT_BOOL *pVal)
//{
//  *pVal=m_bBarOutline;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_BarOutline(VARIANT_BOOL newVal)
//{
//  m_bBarOutline=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_BarOutlineColor(OLE_COLOR *pVal)
//{
//  *pVal=m_clrBarOutline;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_BarOutlineColor(OLE_COLOR newVal)
//{
//  m_clrBarOutline=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_Scale(double *pVal)
//{
//  *pVal=m_fScale;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_Scale(double newVal)
//{
//  m_fScale=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::SetCustomUnit(double factor, BSTR name, BSTR short_name)
//{
////If the user wants to use customer unit, then the map unit and the bar unit will be the same
////Map Unit
//  if (factor<=0.0) //factor should be >0
//    factor=1.0; 
//  m_fMapUnitFactor=factor;
//  wcstombs(m_strMapUnitName, name, MaxNameLength);    
//  wcstombs(m_strMapUnitShortName, short_name, MaxNameLength);    

////Bar Unit   
//  m_fBarUnitFactor=factor;
//  wcstombs(m_strBarUnitName, name, MaxNameLength);    
//  wcstombs(m_strBarUnitShortName, short_name, MaxNameLength);    

//  CalcScale();
//  FireViewChange();
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::GetMapUnitInfo(double *factor, BSTR *name, BSTR *short_name)
//{
//  wchar_t wc_buffer[MaxNameLength+1];
//  *factor=m_fMapUnitFactor;
//  mbstowcs(wc_buffer, m_strMapUnitName, MaxNameLength);
//  *name=SysAllocString(wc_buffer);
//  mbstowcs(wc_buffer, m_strMapUnitShortName, MaxNameLength);
//  *short_name=SysAllocString(wc_buffer);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::GetBarUnitInfo(double *factor, BSTR *name, BSTR *short_name)
//{
//  wchar_t wc_buffer[MaxNameLength+1];
//  *factor=m_fBarUnitFactor;
//  mbstowcs(wc_buffer, m_strBarUnitName, MaxNameLength);
//  *name=SysAllocString(wc_buffer);
//  mbstowcs(wc_buffer, m_strBarUnitShortName, MaxNameLength);
//  *short_name=SysAllocString(wc_buffer);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::SetScaleD(double lon1, double lon2, double lat, long WidthInPixel)
//{
//  m_fLon1=lon1;
//  m_fLon2=lon2;
//  m_fLat =lat;
//  m_nPageWidth=WidthInPixel;
//  CalcScale();
//  FireViewChange();
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::SetScale(double MapWidth, long WidthInPixel)
//{
//  m_fMapWidth=MapWidth;
//  m_nPageWidth=WidthInPixel;
//  CalcScale();
//  FireViewChange();
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_NumTics(int *pVal)
//{
//  *pVal=m_nNumTics;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_NumTics(int newVal)
//{
//  m_nNumTics=newVal;
//  if (m_nNumTics<=0)
//    m_nNumTics=1;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_ScaleText(sbScaleText *pVal)
//{
//    // TODO: Add your implementation code here
//  *pVal=m_sbScaleText;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_ScaleText(sbScaleText newVal)
//{
//    // TODO: Add your implementation code here
//  m_sbScaleText=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_BorderStyle(WSPenStyle *pVal)
//{
//    // TODO: Add your implementation code here
//  *pVal=m_nBorderStyle;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_BorderStyle(WSPenStyle newVal)
//{
//    // TODO: Add your implementation code here
//  m_nBorderStyle=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//void CWScaleBar::ScaleBarText(double scale, sbScaleText scale_text, char *buffer, int buffer_size)
//{
//  ostrstream s;
//  char buf[MAX_LEN];
//  int precision=0;
//  int Magnitude; 

////set the precision. Keep the first 5 (ScalePrecisionDigits) digits. 
//  if (scale>0) {
//    Magnitude=(int)(log10(scale));
//    precision=ScalePrecisionDigits - Magnitude;
//    if (precision<0)
//      precision=0;
//    if (Magnitude>=2)  //don't show the precision if the scale is less than than 1:100 (e.g. 1:1000)
//      precision=0;
//  }
//  s.precision(precision);
////set output format.
//  s.setf(ios::left, ios::adjustfield);
//  s.setf(ios::fixed, ios::floatfield);

////text
//  if (scale_text==ws_stUnitsOnly) { //unit
//    s << m_strBarUnitName;
//  }
//  else if (scale_text==ws_stFraction) { //scale(1:xxxx)
//    scale=FormatRealScale(scale);
//    s << scale << ends;
//    memset(buf, 0, MAX_LEN);
//    if (m_bFormatNumber) {
//      m_locale.FormatNumberStr(s.str(), buf, MAX_LEN-1);
//    }
//    else {
//      strncpy(buf, s.str(), MAX_LEN-1);
//    }
//    s.rdbuf()->freeze(false);
//    s.seekp(ios::beg);    
//    s << "1:";
//    s << buf;
//  }
//  s << ends;
////copy the string to the buffer.
//  strncpy(buffer, s.str(), buffer_size-1);
//  s.rdbuf()->freeze(false);
//}

//STDMETHODIMP CWScaleBar::get_BarStyle(ScaleBarStyle *pVal)
//{
//    // TODO: Add your implementation code here
//  *pVal=m_barStyle;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_BarStyle(ScaleBarStyle newVal)
//{
//    // TODO: Add your implementation code here
//  m_barStyle=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

///*
//STDMETHODIMP CWScaleBar::get_TransparentBG(VARIANT_BOOL *pVal)
//{
//    // TODO: Add your implementation code here
//  *pVal=m_bTransparentBG;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_TransparentBG(VARIANT_BOOL newVal)
//{
//    // TODO: Add your implementation code here
//  m_bTransparentBG=newVal;
//  FireViewChange();
//  SetDirty(TRUE);

//  if (IsWindow()) //take effect for the transparency
//    SetWindowPos(HWND_TOP,0,0,0,0,SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

//  return S_OK;
//}
//*/

//void CWScaleBar::SetTransparentWnd()
//{
//  LONG style;
//  if (IsWindow()) {
//    style=GetWindowLong(GWL_EXSTYLE);
//    style=style | WS_EX_TRANSPARENT;
//    SetWindowLong(GWL_EXSTYLE, style);
//    SetWindowPos(HWND_TOP,0,0,0,0,SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
//  }
//}

////Draw the scalebar directly on the device. (x1, y1, x2, y2) is the drawing area (in pixels). 
//STDMETHODIMP CWScaleBar::RenderOnDC(long hdc, long x1, long y1, long x2, long y2)
//{
//  RECT rc;
//  rc.left  =x1;
//  rc.top   =y1;
//  rc.right =x2;
//  rc.bottom=y2;
//  DrawTheControl((HDC__ *)hdc, rc);  
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_FormatNumber(VARIANT_BOOL *pVal)
//{
//    // TODO: Add your implementation code here
//  *pVal=m_bFormatNumber;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_FormatNumber(VARIANT_BOOL newVal)
//{
//    // TODO: Add your implementation code here
//  m_bFormatNumber=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

///*
//LRESULT CWScaleBar::OnCreate(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
//{
//    // TODO : Add Code for message handler. Call DefWindowProc if necessary.
//    SetTransparentWnd();
//    return 0;
//}*/

//STDMETHODIMP CWScaleBar::get_MarginLeft(int *pVal)
//{
//    // TODO: Add your implementation code here
//  *pVal=m_nMarginLeft;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_MarginLeft(int newVal)
//{
//    // TODO: Add your implementation code here
//  m_nMarginLeft=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::get_MarginRight(int *pVal)
//{
//    // TODO: Add your implementation code here
//  *pVal=m_nMarginRight;
//  return S_OK;
//}

//STDMETHODIMP CWScaleBar::put_MarginRight(int newVal)
//{
//    // TODO: Add your implementation code here
//  m_nMarginRight=newVal;
//  FireViewChange();
//  SetDirty(TRUE);
//  return S_OK;
//}

//    }
//}