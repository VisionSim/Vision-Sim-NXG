#!/bin/sh

case "$1" in

  'clean')

    mono Prebuild.exe /clean

  ;;


  'autoclean')

    echo y|mono Prebuild.exe /clean

  ;;



  *)

    mono Prebuild.exe /target vs2019 /file prebuild.xml

  ;;

esac
