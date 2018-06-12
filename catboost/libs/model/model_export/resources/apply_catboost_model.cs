/* Model applicator */
double ApplyCatboostModel(
    List<float> floatFeatures,
    List<string> catFeatures) {
    CatboostModel model = this.CatboostModelStatic;

    if (floatFeatures.count() != model.FloatFeaturesCount)
        throw new ArgumentException();
    if (catFeatures.count() != model.CatFeaturesCount)
        throw new ArgumentException();

    /* Binarize features */
    Listr<byte> binaryFeatures(model.BinaryFeatureCount);
    uint binFeatureIndex = 0;
    {
        /* Binarize float features */
        for (long i = 0; i < model.FloatFeatureBorders.count(); ++i) {
            foreach (float border in  model.FloatFeatureBorders[i]) {
                binaryFeatures[binFeatureIndex] += (byte)(floatFeatures[i] > border);
            }
            ++binFeatureIndex;
        }
    }

    List<int> transposedHash = new List<int>(model.CatFeatureCount);
    for (long i = 0; i < model.CatFeatureCount; ++i) {
        transposedHash[i] = CityHash64(catFeatures[i].c_str(), catFeatures[i].size()) & 0xffffffff;
    }

    if (model.OneHotCatFeatureIndex.count() > 0) {
        /* Binarize one hot cat features */
        Dictionary<int, int> catFeaturePackedIndexes;
        for (uint i = 0; i < model.CatFeatureCount; ++i) {
            catFeaturePackedIndexes[model.CatFeaturesIndex[i]] = i;
        };
        for (uint i = 0; i < model.OneHotCatFeatureIndex.count(); ++i) {
            var catIdx = catFeaturePackedIndexes.at(model.OneHotCatFeatureIndex[i]);
            var hash = transposedHash[catIdx];
            for (uint borderIdx = 0; borderIdx < model.OneHotHashValues[i].count(); ++borderIdx) {
                binaryFeatures[binFeatureIndex] |= (byte)(hash == model.OneHotHashValues[i][borderIdx]) * (borderIdx + 1);
            }
            ++binFeatureIndex;
        }
    }

    if (model.modelCtrs.UsedModelCtrsCount > 0) {
        /* Binarize CTR cat features */
        List<float> ctrs = new List<float>(model.modelCtrs.UsedModelCtrsCount);
        CalcCtrs(model.modelCtrs, binaryFeatures, transposedHash, ctrs);

        for (long i = 0; i < model.CtrFeatureBorders.count(); ++i) {
            foreach (float border in model.CtrFeatureBorders[i]) {
                binaryFeatures[binFeatureIndex] += (byte)(ctrs[i] > border);
            }
            ++binFeatureIndex;
        }
    }

    /* Extract and sum values from trees */
    double result = 0.0;
    uint treeSplitsIndex = 0;
    uint currentTreeLeafValuesIndex = 0;;
    for (uint treeId = 0; treeId < model.TreeCount; ++treeId) {
        const uint currentTreeDepth = model.TreeDepth[treeId];
        uint index = 0;
        for (unsigned int depth = 0; depth < currentTreeDepth; ++depth) {
            const byte borderVal = model.TreeSplitIdxs[treeSplitsIndex + depth];
            const uint featureIndex = model.TreeSplitFeatureIndex[treeSplitsIndex + depth];
            const byte xorMask = model.TreeSplitXorMask[treeSplitsIndex + depth];
            index |= ((binaryFeatures[featureIndex] ^ xorMask) >= borderVal) << depth;
        }
        result += leafValuesPtr[currentTreeLeafValuesIndex  + index];
        treeSplitsIndex += currentTreeDepth;
        currentTreeLeafValuesIndex += (1 << currentTreeDepth);
    }
    return result;
}
