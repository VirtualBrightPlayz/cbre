/*
  Native File Dialog

  User API

  http://www.frogtoss.com/labs
 */

//Modified by juanjp600 to export symbols for dynamic linking

#ifndef _NFD_H
#define _NFD_H

#ifdef BUILDING_LIB
#ifdef _WIN32
#define EXPORT(x) _declspec(dllexport) x  _cdecl
#elif defined __APPLE__
#define EXPORT(x) x  __attribute__((visibility("default")))
#else
#define EXPORT(x) x
#endif
#else
#define EXPORT(x) x
#endif

#ifdef __cplusplus
extern "C" {
#endif

#include <stddef.h>

/* denotes UTF-8 char */
typedef char nfdchar_t;

/* opaque data structure -- see NFD_PathSet_* */
typedef struct {
    nfdchar_t *buf;
    size_t *indices; /* byte offsets into buf */
    size_t count;    /* number of indices into buf */
}nfdpathset_t;

typedef enum {
    NFD_ERROR,       /* programmatic error */
    NFD_OKAY,        /* user pressed okay, or successful return */
    NFD_CANCEL       /* user pressed cancel */
}nfdresult_t;
    

/* nfd_<targetplatform>.c */

/* single file open dialog */    
EXPORT(nfdresult_t) NFD_OpenDialog( const nfdchar_t *filterList,
                            const nfdchar_t *defaultPath,
                            nfdchar_t **outPath );

/* multiple file open dialog */    
EXPORT(nfdresult_t) NFD_OpenDialogMultiple( const nfdchar_t *filterList,
                                    const nfdchar_t *defaultPath,
                                    nfdpathset_t *outPaths );

/* save dialog */
EXPORT(nfdresult_t) NFD_SaveDialog( const nfdchar_t *filterList,
                            const nfdchar_t *defaultPath,
                            nfdchar_t **outPath );


/* select folder dialog */
EXPORT(nfdresult_t) NFD_PickFolder( const nfdchar_t *defaultPath,
                            nfdchar_t **outPath);

/* nfd_common.c */

/* get last error -- set when nfdresult_t returns NFD_ERROR */
EXPORT(const char *) NFD_GetError( void );
/* get the number of entries stored in pathSet */
EXPORT(size_t)       NFD_PathSet_GetCount( const nfdpathset_t *pathSet );
/* Get the UTF-8 path at offset index */
EXPORT(nfdchar_t  *) NFD_PathSet_GetPath( const nfdpathset_t *pathSet, size_t index );
/* Free the pathSet */    
EXPORT(void)         NFD_PathSet_Free( nfdpathset_t *pathSet );


#ifdef __cplusplus
}
#endif

#endif
