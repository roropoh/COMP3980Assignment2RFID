#ifndef HEADER_H
#define HEADER_H

#define IDM_CONNECT     100

#include <windowsx.h>
#include <windows.h>
#include <stdio.h>
#include <SkyeTekAPI.h>
#include <conio.h>

DWORD WINAPI ReadThreadFunc(LPVOID);
LRESULT CALLBACK WndProc (HWND, UINT, WPARAM, LPARAM);
BOOL Register (HINSTANCE);
HWND Create (HINSTANCE hInst, int nCmdShow);
void RFID_OnDestroy (HWND hwnd);
BOOL RFID_Connect (HWND hwnd,  int id, HWND hwndCtl, UINT codeNotify);

#endif
