_*NOTE*: The project was born as an ancillary for [DeveelDB](http://github.com/deveel/deveeldb), but since this switched to Irony for generating PL/SQL parsers, CSharpCC became an unmaintained project_

CSharpCC
========

The scope of this project is to port the functionalities provided by JavaCC for the generation of parsers and lexical analyzers (lexers) for .NET projects. The produced code will consist of C# files easily embeddable in projects, without the requirement of any external reference.

Some Points
===========

Performance tests under Java have proven JavaCC produces parsers that are sensibly faster, compared to the other generators, adding the advantage of having smaller footprints in projects, since it is not necessary to reference entire librares, but only few files (YACC style).

History
=======

To cover such lack of support in .NET environments, a first attempt was done (by me), creating a Java project named _CSharpCC_ that was adjusted (not ported yet) to generate C# files. Although the project succesfully accomplished its goal, it has always been a pain to maintain it and to involve further contributors. Furthermore, because of some lacks in the original JavaCC, the application has never been too much scalable.
