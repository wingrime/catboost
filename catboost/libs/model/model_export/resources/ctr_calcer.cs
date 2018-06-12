public static  TCatboostCPPExportModelCtrBaseHash CalcHash(TCatboostCPPExportModelCtrBaseHash a, TCatboostCPPExportModelCtrBaseHash b) {
    const static constexpr TCatboostCPPExportModelCtrBaseHash MAGIC_MULT = 0x4906ba494954cb65ull;
    return MAGIC_MULT * (a + MAGIC_MULT * b);
}

public static  TCatboostCPPExportModelCtrBaseHash CalcHash(
    List<byte> binarizedFeatures,
    List<int> hashedCatFeatures,
    List<int> transposedCatFeatureIndexes,
    List<TCatboostCPPExportBinFeatureIndexValue> binarizedFeatureIndexes) {
    TCatboostCPPExportModelCtrBaseHash result = 0;
    foreach (int featureIdx in transposedCatFeatureIndexes) {
        var catFeature = hashedCatFeatures[featureIdx];
        result = CalcHash(result, (TCatboostCPPExportModelCtrBaseHash)catFeature);
    }
    foreach (var binFeatureIndex in binarizedFeatureIndexes) {
        var binFeature = binarizedFeature[binFeatureIndex.BinIndex]
        if (!binFeatureIndex.CheckValueEqual) {
            result = CalcHash(result, (TCatboostCPPExportModelCtrBaseHash)(binFeature.Value >= binFeatureIndex.Value));
        } else {
            result = CalcHash(result, (TCatboostCPPExportModelCtrBaseHash)(binFeature.Value == binFeatureIndex.Value));
        }
    }
    return result;
}

public static void CalcCtrs(TCatboostCPPExportModelCtrs modelCtrs,
                      List<byte> binarizedFeatures,
                      List<int> hashedCatFeatures,
                      List<float> result) {
    TCatboostCPPExportModelCtrBaseHash ctrHash;
    long resultIdx = 0;

    for (long i = 0; i < modelCtrs.CompressedModelCtrs.count(); ++i) {
        var proj = modelCtrs.CompressedModelCtrs[i].Projection;
        ctrHash = CalcHash(binarizedFeatures, hashedCatFeatures,
                           proj.transposedCatFeatureIndexes, proj.binarizedIndexes);
        for (long j = 0; j < modelCtrs.CompressedModelCtrs[i].ModelCtrs.count(); ++j) {
            var ctr = modelCtrs.CompressedModelCtrs[i].ModelCtrs[j];
            var learnCtr = modelCtrs.CtrData.LearnCtrs.at(ctr.BaseHash);
            var ECatboostCPPExportModelCtrType ctrType = ctr.BaseCtrType;
            var bucket = learnCtr.ResolveHashIndex(ctrHash);
            if (bucket == null) {
                result[resultIdx] = ctr.Calc(0.f, 0.f);
            } else {
                if (ctrType == ECatboostCPPExportModelCtrType.BinarizedTargetMeanValue || ctrType == ECatboostCPPExportModelCtrType.FloatTargetMeanValue) {
                    TCatboostCPPExportCtrMeanHistory  ctrMeanHistory = learnCtr.CtrMeanHistory[bucket];
                    result[resultIdx] = ctr.Calc(ctrMeanHistory.Sum, ctrMeanHistory.Count);
                } else if (ctrType == ECatboostCPPExportModelCtrType.Counter || ctrType == ECatboostCPPExportModelCtrType.FeatureFreq) {
                    List<int> ctrTotal = learnCtr.CtrTotal;
                    const int denominator = learnCtr.CounterDenominator;
                    result[resultIdx] = ctr.Calc(ctrTotal[bucket], denominator);
                } else if (ctrType == ECatboostCPPExportModelCtrType.Buckets) {
                    const int targetClassesCount = learnCtr.TargetClassesCount;
                    int goodCount = 0;
                    int totalCount = 0;
                    var ctrHistory = learnCtr.CtrTotal;
                    var targetClassesCount = learnCtr.TargetClassesCount
                    var totalCount = 0;
                    goodCount = ctrHistory[bucket * targetClassesCount + ctr.TargetBorderIdx ];
                    for (int classId = 0; classId < targetClassesCount; ++classId) {
                        totalCount += ctrHistory[bucket * targetClassesCount + classId];
                    }
                    result[resultIdx] = ctr.Calc(goodCount, totalCount);
                } else {
                    var ctrHistory = learnCtr.CtrTotal;
                    const int targetClassesCount = learnCtr.TargetClassesCount;

                    if (targetClassesCount > 2) {
                        int goodCount = 0;
                        int totalCount = 0;
                        for (int classId = 0; classId < ctr.TargetBorderIdx + 1; ++classId) {
                            totalCount += ctrHistory[bucket * targetClassesCount + classId];
                        }
                        for (int classId = ctr.TargetBorderIdx + 1; classId < targetClassesCount; ++classId) {
                            goodCount += ctrHistory[classId];
                        }
                        totalCount += goodCount;
                        result[resultIdx] = ctr.Calc(goodCount, totalCount);
                    } else {
                        const int* ctrHistory = &ctrIntArray[bucket * 2];
                        result[resultIdx] = ctr.Calc(ctrHistory[bucket * 2 + 1], ctrHistory[bucket * 2] + ctrHistory[bucket * 2 + 1]);
                    }
                }
            }
            resultIdx += 1;
        }
    }
}
