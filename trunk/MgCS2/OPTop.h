/*--------------------------------------------------------------------------*/
/*----------------------------- File OPTop.h -------------------------------*/
/*--------------------------------------------------------------------------*/
/** @file
 * Several small macros implementing relation operators (greater
 * than, euqal ...) for FNumber and CNumber, with and without epsilon.
 * 
 * For macros allowing to compare two values, the first one can be
 * plus or minus infinity, but the second should not; for all the
 * other macros, the quantity to be compared can always be +/- INF.
 *
 * \note Two macros EPS_FLOW and EPS_COST must be defined for choosing
 *       between the "plain" and "with epsilon" implementations of the
 *       macros in this file.
 *
 * \warning The parameters of the following macros *must* be either a
 *          variable or a  constant, no other expression is safe.
 *          The choice was dictated by efficiency considerations: the macros
 *          are used at a very low level, complicating them would be costly.
 *
 * \version 3.00
 *
 * \date 13 - 10 - 2004
 *
 * \author Antonio Frangioni \n
 *         Operations Research Group \n
 *         Dipartimento di Informatica \n
 *         Universita' di Pisa \n
 *
 * \author Claudio Gentile \n
 *         Istituto di Analisi di Sistemi e Informatica \n
 *         Consiglio Nazionale delle Ricerche \n
 *         Viale Manzoni, 30 Roma \n
 *
 * Copyright 1996 - 2004
 */
/*--------------------------------------------------------------------------*/
/*--------------------------------------------------------------------------*/
/*--------------------------------------------------------------------------*/

#ifndef __OPTop
 #define __OPTop      // self-identification - endif at the end of the file
 
/*--------------------------------------------------------------------------*/
/*--------------------------------- MACROS ---------------------------------*/
/*--------------------------------------------------------------------------*/
/*--------------------------- Macros for arc flows -------------------------*/
/*--------------------------------------------------------------------------*/
/** @defgroup FLOWS Macros for arc flows
    These macros define comparison operations between two arc flow values,
    (i.e., FNumber) one of which can be zero, with and without "epsilon"
    depending on the value of EPS_FLOW.
    @{ */

#if( EPS_FLOW )
 
 #define FETZ( x , eps ) ( ( x <= eps ) && ( x >= - eps ) )
 ///< x == 0

 #define FGTZ( x , eps ) ( x > eps )
 ///< x > 0

 #define FGEZ( x , eps ) ( x >= - eps )
 ///< x >= 0

 #define FLTZ( x , eps ) ( x < - eps )
 ///< x < 0

 #define FLEZ( x , eps ) ( x <= eps )
 ///< x <= 0

 #define FGT( x , y , eps ) ( x > y + eps )
 ///< x > y

 #define FLT( x , y , eps ) ( x < y - eps )
 ///< x < y

#else

 #define FETZ( x , eps ) ( x == 0 )
 ///< x == 0

 #define FGTZ( x , eps ) ( x > 0 )
 ///< x > 0

 #define FGEZ( x , eps ) ( x >= 0 )
 ///< x >= 0

 #define FLTZ( x , eps ) ( x < 0 )
 ///< x < 0

 #define FLEZ( x , eps ) ( x <= 0 )
 ///< x <= 0

 #define FGT( x , y , eps ) ( x > y )
 ///< x > y

 #define FLT( x , y , eps ) ( x < y )
 ///< x < y

#endif

/*@} -----------------------------------------------------------------------*/
/*--------------------------- Macros for arc costs -------------------------*/
/*--------------------------------------------------------------------------*/
/** @defgroup COSTS Macros for arc costs
    These macros define comparison operations between two arc cost values,
    (i.e., CNumber) one of which can be zero, with and without "epsilon"
    depending on the value of EPS_COST.
    @{ */

#if( EPS_COST )

 #define CETZ( x , eps ) ( ( x <= eps ) && ( x >= - eps ) )
 ///< x == 0

 #define CGTZ( x , eps ) ( x > eps )
 ///< x > 0

 #define CGEZ( x , eps ) ( x >= - eps )
 ///< x >= 0

 #define CLTZ( x , eps ) ( x < - eps )
 ///< x < 0

 #define CLEZ( x , eps ) ( x <= eps )
 ///< x <= 0

 #define CGT( x , y , eps ) ( x > y + eps )
 ///< x > y

 #define CLT( x , y , eps ) ( x < y - eps )
 ///< x < y

#else

 #define CETZ( x , eps ) ( x == 0 )
 ///< x == 0

 #define CGTZ( x , eps ) ( x > 0 )
 ///< x > 0

 #define CGEZ( x , eps ) ( x >= 0 )
 ///< x >= 0

 #define CLTZ( x , eps ) ( x < 0 )
 ///< x < 0

 #define CLEZ( x , eps ) ( x <= 0 )
 ///< x <= 0

 #define CGT( x , y , eps ) ( x > y )
 ///< x > y

 #define CLT( x , y , eps ) ( x < y )
 ///< x < y

#endif

/*@} -----------------------------------------------------------------------*/

#endif  /* OPTop.h included */

/*--------------------------------------------------------------------------*/
/*--------------------------- End File OPTop.h -----------------------------*/
/*--------------------------------------------------------------------------*/
