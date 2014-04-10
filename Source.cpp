/*---------------------------------------------------------------------------------------------------
-- SOURCE FILE: Source.cpp - An application that will read RFID tags from the RFID reader given, and
--                               display the reader name, as well as continuously print out the tags
--                               being read.
--
-- PROGRAM: RFIDreader
--
-- FUNCTIONS:
-- VOID sendToPort(WPARAM wParam);
-- VOID readFromPort(HWND hwnd);
-- DWORD WINAPI readThreadFunc(LPVOID hwnd);
--
-- DATE: October 10, 2013
--
-- REVISIONS: October 10, 2013
--
-- DESIGNER:    Mat Siwoski A00758640
--              Robin Hsieh A00657820
--
-- PROGRAMMER:  Mat Siwoski A00758640
--              Robin Hsieh A00657820
--
-- NOTES:
-- To use this application, you must have the reader plugged in properly through the usb ports.
-- Once opened, clicking on the connect will start the program up. Once the reader name has displayed 
-- properly on screen, it will start printing all the tags being read by the reader.
--
---------------------------------------------------------------------------------------------------*/
#define STRICT
#include "Header.h"

#pragma comment( lib, "stapi" )
#pragma warning (disable: 4096)

HANDLE hThrd;
DWORD threadId;
HWND hwnd;
HDC hdc;
TCHAR* str;
LPSKYETEK_DEVICE *lpDevice    = NULL; 
LPSKYETEK_READER *lpReader    = NULL;
LPSKYETEK_DATA lpData        = NULL; 
LPSKYETEK_TAG *lptags        = NULL;
SKYETEK_STATUS st;
int numDevices                = 0;
int numReaders                = 0;
int ix                        = 0;
static TCHAR Name[]            = TEXT("RFID Reader");
static HWND MainWindow;
static HINSTANCE hInstance;

int WINAPI WinMain (HINSTANCE hInst, HINSTANCE hprevInstance, LPSTR lspszCmdParam, int nCmdShow)
{
    MSG Msg;
    if(!hprevInstance){
        if (!Register(hInst))
            return FALSE;
    }
    
    MainWindow = Create (hInst, nCmdShow);
       if (!MainWindow)
          return FALSE;

    while (GetMessage (&Msg, NULL, 0, 0))
    {
        TranslateMessage (&Msg);
        DispatchMessage (&Msg);
    }
    return Msg.wParam;
}

LRESULT CALLBACK WndProc (HWND hwnd, UINT Message, WPARAM wParam, LPARAM lParam){
    switch (Message)
    {
        HANDLE_MSG(hwnd, WM_COMMAND, RFID_Connect);
        HANDLE_MSG (hwnd, WM_DESTROY, RFID_OnDestroy);
		default:
            return DefWindowProc (hwnd, Message, wParam, lParam);
    }
}

void RFID_OnDestroy (HWND hwnd){
   PostQuitMessage(0);
}

/*------------------------------------------------------------------------------------------------------------------
-- FUNCTION: Register
--
-- DATE:                October 12, 2013
--
-- REVISIONS:           (Date and Description)
--
-- DESIGNER:            Mat Siwoski A00758640
--
-- PROGRAMMER:          Mat Siwoski A00758640
--
-- INTERFACE:           BOOL Register(HINSTANCE hInst)
--                                  HINSTANCE hInst:    the hInstance variable of the window
--
-- RETURNS:             returns the RegisterClass of the window
--
-- NOTES:
-- Call this function to register all the window settings
----------------------------------------------------------------------------------------------------------------------*/
BOOL Register(HINSTANCE hInst){
    WNDCLASS Wcl;

    memset (&Wcl, 0, sizeof(WNDCLASS));
    Wcl.style = CS_HREDRAW | CS_VREDRAW;
    Wcl.hIcon = LoadIcon(NULL, IDI_APPLICATION); // large icon 
    Wcl.hCursor = LoadCursor(NULL, IDC_ARROW);  // cursor style
    Wcl.lpfnWndProc = WndProc;
    Wcl.hInstance = hInst;
    Wcl.hbrBackground = (HBRUSH) GetStockObject(WHITE_BRUSH); //white background
    Wcl.lpszClassName = Name;
    Wcl.lpszMenuName = TEXT("MYMENU"); // The menu Class
    Wcl.cbClsExtra = 0;      // no extra memory needed
    Wcl.cbWndExtra = 0; 

    return (RegisterClass (&Wcl) != 0);
}

/*------------------------------------------------------------------------------------------------------------------
-- FUNCTION: Create
--
-- DATE:                October 12, 2013
--
-- REVISIONS:           (Date and Description)
--
-- DESIGNER:            Mat Siwoski A00758640
--
-- PROGRAMMER:          Mat Siwoski A00758640
--
-- INTERFACE:           HWND Create (HINSTANCE hInst, int nCmdShow)
--                                  HINSTANCE hInst:    the hInstance variable of the window
--                                  itn nCmdShow:       showing of the command
--
-- RETURNS:             returns a handle to the window created
--
-- NOTES:
-- Call this function to create a new window
----------------------------------------------------------------------------------------------------------------------*/
HWND Create (HINSTANCE hInst, int nCmdShow)
{
     hInstance = hInst;

	 hwnd = CreateWindow (Name, Name, WS_OVERLAPPEDWINDOW|WS_VSCROLL, 10, 10,
        600, 400, NULL, NULL, hInst, NULL);
  

   if (hwnd == NULL)
       return hwnd;

   nCmdShow = SW_SHOW;

   ShowWindow (hwnd, nCmdShow);
   UpdateWindow (hwnd);

   return hwnd;
}

/*------------------------------------------------------------------------------------------------------------------
-- FUNCTION: RFID_Connect
--
-- DATE:                October 12, 2013
--
-- REVISIONS:           (Date and Description)
--
-- DESIGNER:            Mat Siwoski A00758640
--                      Robin Hsieh A00657820
--
-- PROGRAMMER:          Robin Hsieh A00657820
--
-- INTERFACE:           BOOL RFID_Connect (HWND hwnd, int id, HWND hwndCtl, UINT codeNotify)
--                                  HWND hwnd:      the handle of the window
--                                  int id:         the switch id whenever it gets a message from WM_COMMAND
--                                  HWND hwndCtl   
--                                  UINT codeNotify
--
-- RETURNS:             A boolean to see if the device, and the reader has been successfully discovered.
--
-- NOTES:
-- Call this function to discover all the devices and the readers, and create the thread.
----------------------------------------------------------------------------------------------------------------------*/
BOOL RFID_Connect (HWND hwnd, int id, HWND hwndCtl, UINT codeNotify){
    switch(id){
        case IDM_CONNECT:
            if(numDevices = SkyeTek_DiscoverDevices(&lpDevice) > 0)
            {
                if((numReaders = SkyeTek_DiscoverReaders(lpDevice, numDevices, &lpReader)) < 1)
                {
                    MessageBox(NULL, TEXT("No reader is connected"), TEXT("No Reader is Connected"), MB_OK);
                    return false;
                }
            }                
            hThrd = CreateThread(NULL, 0, ReadThreadFunc, hwnd, 0, &threadId );

            break;
    }
    return true;
}

/*------------------------------------------------------------------------------------------------------------------
-- FUNCTION: ReadThreadFunc
--
-- DATE:                October 12, 2013
--
-- REVISIONS:           (Date and Description)
--
-- DESIGNER:            Mat Siwoski A00758640
--                      Robin Hsieh A00657820
--
-- PROGRAMMER:          Mat Siwoski A00758640
--
-- INTERFACE:           DWORD WINAPI ReadThreadFunc(LPVOID n)
--                                  LPVOID n
--
-- RETURNS:             Returns the 0 after the process has been completed.
--
-- NOTES:
-- Call this function when the application needs a new thread. This thread will deal with the event handling, 
-- read from the tag, and print it out.
----------------------------------------------------------------------------------------------------------------------*/
DWORD WINAPI ReadThreadFunc(LPVOID n)
{
    unsigned short count = 0;
    int charPos = 15;
    int countTag = 1;
	int y = 0;
    TCHAR* previousStr;
    hdc = GetDC ((HWND)hwnd);
    LPSKYETEK_READER lpReaderEvent = lpReader[0];
    SetEvent(lpReaderEvent);
    str = lpReader[0]->readerName;
    TextOut(hdc, 0,0, str, 50);
    while(numReaders)
    {
        WaitForSingleObject(lpReaderEvent, INFINITE);
        SkyeTek_GetTags(lpReader[0], AUTO_DETECT, &lptags, &count);
        if(count > 0 )
        {
			if(countTag > 21)
			{
				countTag = 1;
			}
            str = SkyeTek_GetStringFromID(lptags[0]->id);
            TextOut(hdc, 0, charPos*countTag, str, 24);
			++countTag;
            SkyeTek_FreeTag(lptags[0]);
        }    
    }
    SkyeTek_FreeDevices(lpDevice, numDevices);
    return 0;
}