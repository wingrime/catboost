LIBRARY()



SRCS(
    cpp_exporter.cpp
    export_helpers.cpp
    model_exporter.cpp
    python_exporter.cpp
    csharp_exporter.cpp
)

PEERDIR(
    catboost/libs/ctr_description
    catboost/libs/model/flatbuffers
    library/resource
)

RESOURCE(
    catboost/libs/model/model_export/resources/apply_catboost_model.py catboost_model_export_python_model_applicator
    catboost/libs/model/model_export/resources/ctr_structs.py catboost_model_export_python_ctr_structs
    catboost/libs/model/model_export/resources/ctr_calcer.py catboost_model_export_python_ctr_calcer
    catboost/libs/model/model_export/resources/apply_catboost_model.cpp catboost_model_export_cpp_model_applicator
    catboost/libs/model/model_export/resources/ctr_structs.cpp catboost_model_export_cpp_ctr_structs
    catboost/libs/model/model_export/resources/ctr_calcer.cpp catboost_model_export_cpp_ctr_calcer
    catboost/libs/model/model_export/resources/ctr_structs.cs catboost_model_export_csharp_ctr_structs
    catboost/libs/model/model_export/resources/ctr_calcer.cs catboost_model_export_csharp_ctr_calcer
)

END()
