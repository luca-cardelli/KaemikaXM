using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Kaemika {

    // Platform-independent handler for the unique Score instance
    public abstract class KScoreHandler {

        private static KTouchable scoreControl;               // <<============== the only score GUI panel, registered from platforms when the GUI loads

        public static bool showInfluences = false;
        public static SKRect influenceRect = new SKRect(0, 0, 0, 0);

        public static void Register(KTouchable control) {
            scoreControl = control;
        }

        // scoreControl handling

        public static void DoInvalidate() {
            if (scoreControl == null) return;
            scoreControl.DoInvalidate();
        }

        public static void ScoreUpdate() {
            if (scoreControl == null) return;
            if (Exec.lastExecution != null && Exec.lastExecution.lastCRN != null) 
                score = new KScore(Exec.lastExecution.lastCRN, Exec.lastExecution.style);
            KScoreHandler.scoreControl.DoInvalidate();
        }

        public static void OnTouchTapOrMouseMove(Action<SKPoint> action) { if (scoreControl != null) scoreControl.OnTouchTapOrMouseMove(action); }
        public static void OnTouchDoubletapOrMouseClick(Action<SKPoint> action) { if (scoreControl != null) scoreControl.OnTouchDoubletapOrMouseClick(action); }
        public static void OnTouchSwipeOrMouseDrag(Action<SKPoint, SKPoint> action) { if (scoreControl != null) scoreControl.OnTouchSwipeOrMouseDrag(action); }
        public static void OnTouchSwipeOrMouseDragEnd(Action<SKPoint, SKPoint> action) { if (scoreControl != null) scoreControl.OnTouchSwipeOrMouseDragEnd(action); }

        private static bool visible = true;
        public static void ScoreHide() {
            if (scoreControl == null) return;
            visible = false;
            KScoreHandler.scoreControl.DoHide();
        }
        public static void ScoreShow() {
            if (scoreControl == null) return;
            visible = true;
            KScoreHandler.scoreControl.DoShow();
            KScoreHandler.scoreControl.DoInvalidate();
        }

        // score handling

        private static KScore score = null;                     // <<============== the only KScore instance at any given time

        public static void ScoreClear() {
            if (score == null) return;
            score = null;
            manualPinchPan = Swipe.Id();
            KScoreHandler.DoInvalidate();
        }

        public static SKSize Measure(Colorer colorer, float pointSize)  {
            if (score == null) return new SKSize(0, 0);
            return score.OverallSize();
        }

        public static void Draw(Painter painter, int originX, int originY, int width, int height, bool forcePacked = false) {
            if (visible) {
                if (forcePacked) KControls.packReactions = true; // for those platforms that do not have a menu to set packReactions
                if (score == null) painter.Clear(SKColors.White);
                else score.Draw(painter, originX, originY, width, height);
            }
        }

        public static void DrawOver(Painter painter, int originX, int originY, int width, int height) {
            if (visible) {
                if (score == null) return;
                score.DrawOver(painter, originX, originY, width, height);
            }
        }

        private static Swipe manualPinchPan = Swipe.Id();
        public static Swipe ManualPinchPan() {
            return manualPinchPan;
        }
        public static void SetManualPinchPan(Swipe pinchPan) {
            manualPinchPan = pinchPan;
        }

        public static int SpeciesNo() {
            if (score == null) return 0;
            return score.speciesNo;
        }

    }

    // A KScore instance

    public class KScore {
        public static float textHeight = 12.0f;
        public static float lineWeight = 1.5f;
        public static float Yspacing = 2.0f * textHeight;
        public static float Xspacing = textHeight;
        public static float textMargin = 0.5f * textHeight;
        public static float textToScoreGap = 1.0f * textHeight;
        public static float arrowTailSize = 4.0f;
        public static float arrowHeadSize = 5.0f;
        public static float arrowActivateSize = 10.0f;
        public static float arrowInhibitSize = 10.0f * (2.0f / 3.0f);

        private static Swipe pinchPan = Swipe.Id();
        public static Swipe manualPinchPan = Swipe.Id();
        public int speciesNo = 0;

        // remember the order in which species appear in the score
        private static Dictionary<string, int[]> speciesPermutationCache = new Dictionary<string, int[]>();
        private string speciesPermutationCacheKey;

        private static SpeciesTag draggingSpeciesTag; // the tag we are dragging, null at the end of drag
        private static SKPoint draggingSpeciesTagTo; // where we are dragging it to
        private static int startDragIx;

        private CRN crn;
        private Style style;
        private List<SpeciesTag> speciesTags;
        private int[] speciesPermutation;
        private List<ReactionTag> reactionTags;
        public int scoreWidth; // number of Xspacings for score, set by RemeasureReactionTags
        public int scoreHeight; // number of Yspacings for score, determined by number of species
        public float speciesTextWidth_scaled = 6.0f*Xspacing; // initial guess; iteratively set up during Drawing

        public float canvasOriginX = 0;         // set up during Drawing
        public float canvasOriginY = 0;         // set up during Drawing
        public float scoreOriginX = 50;         // set up during Drawing, displacement from canvasOriginX
        public float scoreOriginY = Yspacing;   // displacement from canvasOriginY

        public KScore(CRN crn, Style style) {
            this.crn = crn;
            this.style = style;
            var stateMap = this.crn.sample.stateMap;
            this.speciesNo = stateMap.species.Count;
            this.speciesTags = new List<SpeciesTag>();
            this.speciesPermutation = new int[speciesNo];
            for (int i = 0; i < speciesNo; i++) {
                this.speciesTags.Add(new SpeciesTag(stateMap.species[i], Gui.FormatUnit(stateMap.Mean(stateMap.species[i].symbol), " ", "M", this.style.numberFormat)));
                this.speciesPermutation[i] = i;
            }
            this.speciesPermutationCacheKey = SpeciesKey(stateMap.species);
            if (speciesPermutationCache.ContainsKey(speciesPermutationCacheKey)) this.speciesPermutation = speciesPermutationCache[speciesPermutationCacheKey];
            this.reactionTags = new List<ReactionTag>();
            foreach (ReactionValue reaction in this.crn.reactions)
                this.reactionTags.Add(new ReactionTag(reaction, stateMap.species, KControls.scoreStyle));
            this.scoreWidth = RemeasureReactionTags();
            this.scoreHeight = Math.Max(0, speciesNo - 1);
            KScoreHandler.OnTouchTapOrMouseMove(OnMouseMove);
            KScoreHandler.OnTouchDoubletapOrMouseClick(OnMouseClick);
            KScoreHandler.OnTouchSwipeOrMouseDrag(OnMouseDrag);
            KScoreHandler.OnTouchSwipeOrMouseDragEnd(OnMouseDragEnd);
        }

        public static void DrawRoundRect(Painter painter, SKRect rect, float cornerTL, float cornerTR, float cornerBL, float cornerBR, SKPaint paint) { //assume rect and corner already scaled
            painter.DrawSpline(new List<SKPoint> { 
                new SKPoint(rect.Left, rect.Top + cornerTL), new SKPoint(rect.Left, rect.Top), new SKPoint(rect.Left + cornerTL, rect.Top), 
                new SKPoint(rect.Right - cornerTR, rect.Top), new SKPoint(rect.Right, rect.Top), new SKPoint(rect.Right, rect.Top + cornerTR),
                new SKPoint(rect.Right, rect.Bottom - cornerBR), new SKPoint(rect.Right, rect.Bottom), new SKPoint(rect.Right - cornerBR, rect.Bottom),
                new SKPoint(rect.Left + cornerBL, rect.Bottom), new SKPoint(rect.Left, rect.Bottom), new SKPoint(rect.Left, rect.Bottom - cornerBL),
            }, paint);
        }

        public static float Display(Painter painter, string text, SKPoint point, SKPaint paint, SKPaint back, Swipe pinchPan) {
            SKRect measure = painter.MeasureText(text, paint);
            measure.Inflate(pinchPan % 4, pinchPan % 4);
            SKRect display = pinchPan % new SKRect(point.X + measure.Left / pinchPan.scale, point.Y + measure.Top / pinchPan.scale, point.X + measure.Right / pinchPan.scale, point.Y + measure.Bottom / pinchPan.scale);
            float corner = pinchPan % 8;
            if (back != null) DrawRoundRect(painter, display, corner, corner, corner, corner, back);
            painter.DrawText(text, pinchPan % point, paint);
            return measure.Width / pinchPan.scale;
        }

        public static void Display(Painter painter, string text, SKPoint point, SKPaint paint, SKPaint backTop, SKPaint backBot, Swipe pinchPan) {
            SKRect measure = painter.MeasureText(text, paint);
            measure.Inflate(pinchPan % 4, pinchPan % 4);
            SKRect display = pinchPan % new SKRect(point.X + measure.Left / pinchPan.scale, point.Y + measure.Top / pinchPan.scale, point.X + measure.Right / pinchPan.scale, point.Y + measure.Bottom / pinchPan.scale);
            SKRect displayTop = new SKRect(display.Left, display.Top, display.Right, display.Bottom - display.Height/2);
            SKRect displayBot = new SKRect(display.Left, display.Top + display.Height / 2, display.Right, display.Bottom);
            float corner = pinchPan % 8;
            if (backTop != null) DrawRoundRect(painter, displayTop, corner, corner, 0, 0, backTop);
            if (backBot != null) DrawRoundRect(painter, displayBot, 0, 0, corner, corner, backBot);
            painter.DrawText(text, pinchPan % point, paint);
        }

        public string SpeciesKey(List<SpeciesValue> species) {
            string s = "";
            foreach (SpeciesValue sp in species) s += sp.symbol.Format(this.style) + "|";
            return s;
        }

        public int IndexOfSpeciesTag(Symbol species) {
            lock (this.speciesPermutation) {
                for (int i = 0; i < this.speciesPermutation.Length; i++) {
                    var sTag = this.speciesTags[this.speciesPermutation[i]];
                    if (sTag.species.symbol.SameSymbol(species)) return i;
                }
                throw new Error("KScore.IndexOf");
            }
        }

        public SpeciesTag SpeciesTagOfSymbol(Symbol s) {
            lock (this.speciesPermutation) {
                foreach (SpeciesTag tag in this.speciesTags)
                    if (s.SameSymbol(tag.species.symbol)) return tag;
                return null;
            }
        }

        public float RemeasureSpeciesTags(Colorer colorer, SKPaint paint) {
            lock (this.speciesPermutation) {
                float maxWidth_scaled = 0.0f;
                for (int i = 0; i < this.speciesPermutation.Length; i++) {
                    var tag = this.speciesTags[this.speciesPermutation[i]];
                    tag.level = i;
                    tag.format = tag.species.Format(this.style);
                    tag.SetMeasures(colorer.MeasureText(tag.format, paint));
                    maxWidth_scaled = Math.Max(maxWidth_scaled, tag.measure_scaled.Width);
                }
                return maxWidth_scaled;
            }
        }

        public int RemeasureReactionTags() {
            int X = 0;
            foreach (ReactionTag tag in this.reactionTags) {
                (int width, SymbolMultiset hasCat, SymbolMultiset hasReac, SymbolMultiset hasProd, ReactionMeasure measure) = 
                    MeasureReaction(this.crn.sample.stateMap.species, tag.reaction, tag.scoreStyle);
                if (KControls.packReactions) { // bin packing allocation
                    X = BinPacking(tag, X, measure.coreMeasure.stem.loHitIx, measure.coreMeasure.stem.hiHitIx, width); 
                } else { // sequential allocation
                    X = Sequential(tag, X, measure.coreMeasure.stem.loHitIx, measure.coreMeasure.stem.hiHitIx, width);
                }
                tag.measure = measure;
                tag.hasCat = hasCat;
                tag.hasReac = hasReac;
                tag.hasProd = hasProd;
            }
            X -= 1;
            return X;
        }

        public int Sequential(ReactionTag newTag, int upTo, int loIx, int hiIx, int w) {
            newTag.SetMeasures(upTo, loIx, upTo + w, hiIx);
            return upTo + w + 1;
        }

        public int BinPacking(ReactionTag newTag, int upTo, int loIx, int hiIx, int w) {
            for (int i = 0; i < upTo; i++) { // w can be zero for straight-vertical reactions
                SKRect tryRect = new SKRect(i, loIx, i + w + 1, hiIx); // hence we add to width 1 here
                bool noConflict = true;
                foreach (ReactionTag tag in this.reactionTags) {
                    if (newTag == tag) break;  // don't go past the tags that have been allocated
                    SKRect conflictRect = tag.SpanRect();          // and also add 1 to width of the SpanRect
                    if (conflictRect.IntersectsWith(tryRect)) { noConflict = false; break; }
                }
                if (noConflict) { 
                    newTag.SetMeasures(i, loIx, i + w, hiIx);
                    return Math.Max(upTo, i + w + 1); // we extended up to there
                }
            }
            newTag.SetMeasures(upTo, loIx, upTo + w, hiIx);
            return upTo + w + 1; // we extended beyond upTo
        }

        public void ReorderSpeciesTags(int[] speciesPermutation, SpeciesTag moved, int byLevels) {
            lock (speciesPermutation) {
                int toLevel = moved.level + byLevels;
                if (toLevel < 0) toLevel = 0;
                if (toLevel >= speciesPermutation.Length) toLevel = speciesPermutation.Length-1;
                int fromLevel = moved.level;
                if (toLevel > fromLevel) {
                    int save = speciesPermutation[fromLevel];
                    for (int i = fromLevel; i < toLevel; i++) speciesPermutation[i] = speciesPermutation[i + 1];
                    speciesPermutation[toLevel] = save;
                }
                if (fromLevel > toLevel) {
                    int save = speciesPermutation[fromLevel];
                    for (int i = fromLevel; i > toLevel; i--) speciesPermutation[i] = speciesPermutation[i - 1];
                    speciesPermutation[toLevel] = save;
                }
                speciesPermutationCache[this.speciesPermutationCacheKey] = (int[])this.speciesPermutation.Clone();
                for (int i = 0; i < this.speciesPermutation.Length; i++) {
                    var tag = this.speciesTags[this.speciesPermutation[i]];
                    tag.level = i;
                }
            }
        }

        public void HiliteSpecies(SpeciesTag sTag) {
            sTag.hilite = true;
        }

        public void HiliteReaction(ReactionTag rTag) {
            rTag.hilite = true;
            foreach (SpeciesValue s in rTag.species) {
                SpeciesTag sTag = SpeciesTagOfSymbol(s.symbol);
                sTag.hiliteCat = rTag.hasCat.Has(s.symbol);
                sTag.hiliteReac = rTag.hasReac.Has(s.symbol);
                sTag.hiliteProd = rTag.hasProd.Has(s.symbol);
            }
            if (KScoreHandler.showInfluences) {
                foreach (ReactionTag otherTag in this.reactionTags) {
                    if (otherTag == rTag) {
                        foreach (Symbol s in rTag.hasProd.ToSet()) {
                            if (rTag.hasReac.Has(s)) rTag.hiliteCatPlus = true; // for Mozart
                            if (rTag.hasCat.Has(s)) rTag.hiliteCatPlus = true; // for Bach
                        }
                        foreach (Symbol s in rTag.hasReac.ToSet()) {
                            if (rTag.hasCat.Has(s)) rTag.hiliteCatMinus = true; // for Bach
                        }
                    } else {
                        foreach (Symbol s in rTag.hasProd.ToSet()) {
                            if (otherTag.hasReac.Has(s)) otherTag.hiliteProd = true;
                            if (otherTag.hasCat.Has(s)) otherTag.hiliteCatPlus = true;
                        }
                        foreach (Symbol s in rTag.hasReac.ToSet()) {
                            if (otherTag.hasReac.Has(s)) otherTag.hiliteReac = true;
                            if (otherTag.hasCat.Has(s)) otherTag.hiliteCatMinus = true;
                        }
                    }
                }
            }
        }

        private (int currentIx, SpeciesTag tag) SpeciesHit(SKPoint point, bool justHorizontally = false) {
            lock (this.speciesPermutation) {
                for (int i = 0; i < this.speciesPermutation.Length; i++) {
                    SpeciesTag tag = this.speciesTags[this.speciesPermutation[i]];
                    SKRect hitRect = tag.HitRect(canvasOriginX, canvasOriginY + scoreOriginY, KScore.pinchPan);
                    if (justHorizontally && point.Y >= hitRect.Top && point.Y <= hitRect.Bottom) return (i, tag);
                    else if (hitRect.Contains(point)) return (i, tag);
                }
                return (-1,null);
            }
        }

        private void MouseHits(SKPoint point) {
            foreach (ReactionTag tag in this.reactionTags) { tag.hilite = false; tag.hiliteReac = false; tag.hiliteProd = false; tag.hiliteCatPlus = false; tag.hiliteCatMinus = false; }
            lock (speciesPermutation) { foreach (SpeciesTag tag in this.speciesTags) { tag.hilite = false; tag.hiliteCat = false; tag.hiliteReac = false; tag.hiliteProd = false; } }
            ReactionTag reactionHit = null;
            SpeciesTag speciesHit = null;
            foreach (ReactionTag tag in this.reactionTags) {
                bool hit = tag.HitRect(canvasOriginX + scoreOriginX, canvasOriginY + scoreOriginY, KScore.pinchPan).Contains(point);
                if (hit) { reactionHit = tag; break; }
            }
            lock (this.speciesPermutation) {
                foreach (SpeciesTag tag in this.speciesTags) {
                    bool hit = tag.HitRect(canvasOriginX, canvasOriginY + scoreOriginY, KScore.pinchPan).Contains(point);
                    if (hit) { speciesHit = tag; break; }
                }
            }
            if (reactionHit != null) HiliteReaction(reactionHit);
            if (speciesHit != null) HiliteSpecies(speciesHit);
        }

        private void OnMouseMove(SKPoint point) {
            MouseHits(point);
            KScoreHandler.DoInvalidate();
        }

        private void OnMouseClick(SKPoint point) {
            // Reaction hit: switch between Mozart and Bach
            bool rHit = false;
            if (KScoreHandler.influenceRect.Contains(point)) { 
                KScoreHandler.showInfluences = !KScoreHandler.showInfluences; 
                KScoreHandler.DoInvalidate(); 
            }
            foreach (ReactionTag tag in this.reactionTags) {
                rHit = tag.HitRect(scoreOriginX, scoreOriginY, KScore.pinchPan).Contains(point);
                if (rHit && tag.scoreStyle == "Mozart") tag.scoreStyle = "Bach"; 
                else if (rHit && tag.scoreStyle == "Bach") tag.scoreStyle = "Mozart";
                if (rHit) break;
            }
            if (rHit) {
                this.scoreWidth = RemeasureReactionTags();
                MouseHits(point); // update highlight of reaction under the mouse
                KScoreHandler.DoInvalidate();
            }
        }

        private void OnMouseDrag(SKPoint from, SKPoint to) {
            draggingSpeciesTagTo = to; // use to draw the dragged line
            if (draggingSpeciesTag == null) {
                (int fromIx, SpeciesTag fromTag) = SpeciesHit(from);
                startDragIx = fromIx;
                draggingSpeciesTag = fromTag; // use to draw the dragged line
            } else {
                (int toIx, SpeciesTag toTag) = SpeciesHit(to, justHorizontally:true);
                if (toTag != null) {
                    int step = toIx - startDragIx;
                    if (step != 0) {
                        ReorderSpeciesTags(speciesPermutation, draggingSpeciesTag, step);
                        this.scoreWidth = RemeasureReactionTags();
                        startDragIx = toIx; // because we reordered
                    }
                }
            }
            KScoreHandler.DoInvalidate();
        }

        private void OnMouseDragEnd(SKPoint from, SKPoint to) {
            draggingSpeciesTag = null;
            KScoreHandler.DoInvalidate();
        }

        //// Version of species tag dragging with non-incremental redrawing
        //private void OnMouseDrag(SKPoint from, SKPoint to) {
        //    (int fromIx, SpeciesTag fromTag) = SpeciesHit(from);
        //    KScore.draggingSpeciesTag = fromTag; // use do draw the dragged line
        //    KScore.draggingSpeciesTagTo = to;
        //    KScoreHandler.DoInvalidate();
        //}
        //private void OnMouseDragEnd(SKPoint from, SKPoint to) {
        //    if (draggingSpeciesTag != null) {
        //        ReorderSpeciesTags(speciesPermutation, draggingSpeciesTag, (int)Math.Round(((to.Y-from.Y) / KScore.pinchPan.scale) / Yspacing));
        //        draggingSpeciesTag = null;
        //        this.scoreWidth = RemeasureReactionTags();
        //        KScoreHandler.DoInvalidate();
        //    }
        //}

        public static Paints paints = null;

        // ========= MEASURE THE SCORE ======== //

        public class SpeciesMeasure {
            public Symbol species;      // a species that appears in a complex of a reaction
            public int Ix;              // the score index of the species (invalidated by reordering the score)
            public int cardinality;     // the cardinality of the species in a complex
            public bool onStem;         // true if we can compact it onto the stem
            public int catBound;        // 0 if not bound, 1 if already bound from below (high Ix), -1 if already bound from above (low Ix), used to connect catalysts in special cases
            public float stemConnectY;  // the index or half index where species connects to the reaction stem in the Y dimension

            public SpeciesMeasure(Symbol species, int Ix, int cardinality) {
                this.species = species;
                this.Ix = Ix;
                this.cardinality = cardinality;
                this.catBound = 0; // updated later
                //this.onStem // computed later
                //this.stemConnectY // computed later
            }
        }

        public class ComplexMeasure {
            // case1: # (defined=false), case2: a (hiIx=loIx), case3: a+2b+3c (hiIx!=loIx)
            public SymbolMultiset complex; // a species complex to measure
            public Dictionary<Symbol, SpeciesMeasure> set; // the set of species in the complex and their measures
            public bool defined; // whether hiIx and loIx are defined (false in case1)
            public int hiIx; // max score index of species in set, can be undefined: int.MinValue in case1
            public int loIx; // min score index of species in set, can be undefined: int.MaxValue in case1
            public int verSpan; // hiIx - loIx, or zero if not defined (case1); zero also in case2
            public int horSpan; // max cardinality of each multiset, zero if not defined (case1), otherwise nonzero

            public ComplexMeasure(SymbolMultiset complex, KScore kScore) { 
                this.complex = complex;
                this.set = new Dictionary<Symbol, SpeciesMeasure>();
                foreach (Symbol sym in complex.ToSet()) {
                    this.set.Add(sym, new SpeciesMeasure(sym, kScore.IndexOfSpeciesTag(sym), complex.Cardinality(sym)));
                }
                this.hiIx = int.MinValue;
                this.loIx = int.MaxValue;
                foreach (var kvp in set) {
                    this.hiIx = Math.Max(this.hiIx, kvp.Value.Ix);
                    this.loIx = Math.Min(this.loIx, kvp.Value.Ix);
                }
                this.defined = this.hiIx != int.MinValue && this.loIx != int.MaxValue;
                this.verSpan = this.defined ? this.hiIx - this.loIx : 0;
            }

            public void MeasureAsReactants(StemMeasure stem, bool compact) {
                this.horSpan = 0;
                foreach (var kvp in set) {
                    SpeciesMeasure reactant = kvp.Value;
                    reactant.onStem = reactant.Ix == stem.hiIx || reactant.Ix == stem.loIx;
                    this.horSpan = Math.Max(this.horSpan, kvp.Value.cardinality - (reactant.onStem && compact ? 1 : 0));
                }
            }

            public void MeasureAsProducts(StemMeasure stem, bool compact) {
                this.horSpan = 0;
                foreach (var kvp in set) {
                    SpeciesMeasure product = kvp.Value;
                    product.onStem = product.Ix == stem.hiIx || product.Ix == stem.loIx;
                    this.horSpan = Math.Max(this.horSpan, kvp.Value.cardinality - (product.onStem && compact ? 1 : 0));
                }
            }

            public void MeasureAsCatalysts(CoreReactionMeasure coreMeasure) {
                this.horSpan = 0;
                foreach (var kvp in set) {
                    SpeciesMeasure catalyst = kvp.Value;
                    int cardinality = catalyst.cardinality;
                    if (coreMeasure.CatalystOnStem(catalyst.Ix)) cardinality -= 1;
                    this.horSpan = Math.Max(this.horSpan, cardinality);
                }
            }
        }

        public class StemMeasure {
            public bool hasStem; // true, unless # -> #
            public int hiIx;  // hi index of stem (logical, i.e. max of species indexes)
            public int loIx;  // lo index of stem (logical, i.e. min of species indexes)
            public float midIx;  // integers and half-integers
            public bool hasCatStem; // true if there are some catalysts
            public int hiCatIx; // hi index of catalists (logical, i.e. max of species indexes)
            public int loCatIx; // lo index of catalists (logical, i.e. max of species indexes)
            public float midCatIx; // integers and half-integers
            // computed later:
            public float hiDrawnIx; // hi index of stem, as drawn (integer or half integer)
            public float loDrawnIx; // lo index of stem, as drawn (integer or half integer)
            public float midDrawnIx; // middle of stem, as drawn (integer of half-integer)
            public int hiHitIx; // hi index of hitbox around hiDrawnIx 
            public int loHitIx; // lo index of hitbox around loDrawnIx

            public StemMeasure(CoreReactionMeasure measure) {
                this.hasStem = measure.maxIx != int.MinValue && measure.minIx != int.MaxValue; 
                this.hiIx = measure.maxIx;
                this.loIx = measure.minIx;
                this.midIx = ((float)(this.hiIx + this.loIx)) / 2.0f;
                this.hasCatStem = false;
                this.hiCatIx = int.MinValue;
                this.loCatIx = int.MaxValue;
            }

            public StemMeasure(ReactionMeasure measure) {
                this.hasStem = measure.coreMeasure.maxIx != int.MinValue && measure.coreMeasure.minIx != int.MaxValue; 
                this.hiIx = measure.coreMeasure.maxIx;
                this.loIx = measure.coreMeasure.minIx;
                this.midIx = ((float)(this.hiIx + this.loIx)) / 2.0f;
                this.hasCatStem = measure.catalysts.defined;
                this.hiCatIx = measure.catalysts.hiIx;
                this.loCatIx = measure.catalysts.loIx;
                this.midCatIx = ((float)(this.hiCatIx + this.loCatIx)) / 2.0f;
            }
        }

        public class CoreReactionMeasure { // take the measure of a Mozart score reaction
            public ComplexMeasure reactants;
            public ComplexMeasure products;
            public List<SpeciesValue> species;
            public bool defined; // false for case # -> #
            public int maxIx; // max of defined reac and prod Ix, can be undefined if both are: int.MinValue
            public int minIx; // min of defined reac and prod Ix, can be undefined if both are: int.MaxValue
            public int verSpan; // maxIx - minIx, or zero if not defined
            public int width; // reactants.horSpan + products.horSpan; N.B. this is the Mozart width
            public StemMeasure stem;

            public CoreReactionMeasure(List<SpeciesValue> species, ComplexMeasure reactants, ComplexMeasure products) {
                this.reactants = reactants;
                this.products = products;
                this.species = species;
                this.defined = reactants.defined || products.defined;
                this.maxIx = Math.Max(int.MinValue, Math.Max(reactants.hiIx, products.hiIx));
                this.minIx = Math.Min(int.MaxValue, Math.Min(reactants.loIx, products.loIx));
                this.verSpan = this.defined ? this.maxIx - this.minIx : 0;
                this.stem = null;
            }

            public bool CatalystOnStem(int catalystIx) { // compute whether a catalyst can directly connect vertically to a stem, using catBound
                if (reactants.set.Count == 0 && products.set.Count == 0) return true; // nothing to attach to, but can draw it vertically
                if (reactants.set.Count == 0 && products.set.Count == 1) {
                    foreach (var s in products.set) {
                        if (catalystIx > s.Value.Ix && s.Value.catBound != -1) { s.Value.catBound = 1; return true; } // attach from below, prevent further attach from above
                        if (catalystIx < s.Value.Ix && s.Value.catBound != 1) { s.Value.catBound = -1; return true; } // attach from above, prevent further attach from below
                    }
                }
                if (products.set.Count == 0 && reactants.set.Count == 1) {
                    foreach (var s in reactants.set) {
                        if (catalystIx > s.Value.Ix && s.Value.catBound != -1) { s.Value.catBound = 1; return true; } // attach from below, prevent further attach from above
                        if (catalystIx < s.Value.Ix && s.Value.catBound != 1) { s.Value.catBound = -1; return true; } // attach from above, prevent further attach from below
                    }
                }
                return false;
            }

            public void Connect() { // compute the connections between the different elements in logical units; that's all that is needed for drawing
                ComplexMeasure reactants = this.reactants;
                ComplexMeasure products = this.products;
                StemMeasure stem = this.stem;
                if (!stem.hasStem) { // CASE # -> #:  reactants.set.Count == 0 && products.set.Count == 0
                    stem.hiDrawnIx = +0.5f;  // just draws an empty symbol half a space south of the top line by default
                    stem.loDrawnIx = +0.5f;
                    stem.hiHitIx = 1;
                    stem.loHitIx = 0;
                } else if (reactants.set.Count == 1 && products.set.Count == 0) { // CASE a -> #:    reactant.Ix = stem.hiIx = stem.loIx
                    SpeciesMeasure reactant = null; foreach (var kvp in reactants.set) reactant = kvp.Value;
                    bool belowLine = reactant.Ix == 0 || reactant.Ix != species.Count - 1; // keep the bottom row clear
                    int displ = belowLine ? 1 : -1;
                    float connect = reactant.Ix + displ / 2.0f;
                    stem.hiDrawnIx = connect; 
                    stem.loDrawnIx = connect;
                    reactant.stemConnectY = connect;
                    stem.hiHitIx = Math.Max(maxIx, stem.hiIx + displ);
                    stem.loHitIx = Math.Min(minIx, stem.loIx + displ);
                } else if (reactants.set.Count == 0 && products.set.Count == 1) { // CASE # -> a:   product.Ix = stem.hiIx = stem.loIx
                    SpeciesMeasure product = null; foreach (var kvp in products.set) product = kvp.Value;
                    bool belowLine = product.Ix == 0; // keep the top row clear
                    int displ = belowLine ? 1 : -1;
                    float connect = product.Ix + displ / 2.0f;
                    stem.hiDrawnIx = connect;
                    stem.loDrawnIx = connect;
                    product.stemConnectY = connect;
                    stem.hiHitIx = Math.Max(maxIx, stem.hiIx + displ);
                    stem.loHitIx = Math.Min(minIx, stem.loIx + displ);
                } else if (reactants.set.Count == 1 && products.set.Count == 1 && stem.hiIx == stem.loIx) { // CASE a -> a:   reactant.Ix = product.Ix = stem.hiIx = stem.loIx
                    SpeciesMeasure reactant = null; foreach (var kvp in reactants.set) reactant = kvp.Value;
                    SpeciesMeasure product = null; foreach (var kvp in products.set) product = kvp.Value;
                    bool belowLine = reactant.Ix == 0 || reactant.Ix != species.Count - 1; // keep the bottom row clear
                    int displ = belowLine ? 1 : -1;
                    float connect = reactant.Ix + displ / 2.0f;
                    stem.hiDrawnIx = connect;
                    stem.loDrawnIx = connect; 
                    reactant.stemConnectY = connect;
                    product.stemConnectY = connect;
                    stem.hiHitIx = Math.Max(maxIx, stem.hiIx + displ);
                    stem.loHitIx = Math.Min(minIx, stem.loIx + displ);
                } else { // CASE: otherwise nothing can stick out of the min and max of stem, which are now distinct
                    stem.hiDrawnIx = stem.hiIx - 0.5f;
                    stem.loDrawnIx = stem.loIx + 0.5f;
                    stem.hiHitIx = maxIx;
                    stem.loHitIx = minIx;
                    foreach (var kvp in reactants.set) {
                        SpeciesMeasure reactant = kvp.Value;
                        reactant.stemConnectY = reactant.Ix + ((reactant.Ix <= stem.midIx) ? +0.5f : -0.5f);
                    }
                    foreach (var kvp in products.set) {
                        SpeciesMeasure product = kvp.Value;
                        product.stemConnectY = product.Ix + ((product.Ix <= stem.midIx) ? +0.5f : -0.5f);
                    }
                }
                stem.midDrawnIx = ((float)(stem.hiDrawnIx + stem.loDrawnIx)) / 2.0f;
            }

            public void MeasureAndConnect() {
                StemMeasure stem = new StemMeasure(this); // compute hasStem,hiIx,loIx,midIx
                this.stem = stem;
                reactants.MeasureAsReactants(stem, false); // compute onStem,horSpan
                products.MeasureAsProducts(stem, false); // compute onStem,horSpan
                this.width = reactants.horSpan + products.horSpan;

                this.Connect(); // compute connections
            }
        }

        public class ReactionMeasure { // take the measure of a Bach score reaction
            public ReactionValue reaction;
            public ComplexMeasure catalysts; // .defined=false for non-catalytic reactions (cases a -> b with a!=b, etc.)
            public CoreReactionMeasure coreMeasure; // .defined=false for case # -> #, which includes catalytic reactions a -> a, a+b -> a+b, etc.
            public bool defined; // false for case # -> #
            public int maxIx; // max(coreMeasure.maxIx, catMeasure.hiIx) if coreMeasure.defined
            public int minIx; // min(coreMeasure.minIx, catMeasure.loIx) if coreMeasure.defined
            public int maxDrawnIx; // maxIx, but if !coreMesure.defined then max(catMeasure.loIx+1,catMeasure.hiIx) (cases a->a, a+b->a+b, etc. , i.e. drawn UNDER the line for a->a)
            public int minDrawnIx; // minIx, but if !coreMesure.defined then catMeasure.loIx (cases a->a, a+b->a+b, etc.)
            public int width; // if catMeasure undefined (Mozart) then coreMeasure.width, else (Bach) coreMeasure.width + catMeasure.horSpan 

            public ReactionMeasure(ReactionValue reaction, ComplexMeasure catalysts, CoreReactionMeasure coreMeasure) {
                this.reaction = reaction;
                this.catalysts = catalysts;
                this.coreMeasure = coreMeasure;
                this.defined = catalysts.defined || coreMeasure.defined;
                if (coreMeasure.defined && catalysts.defined) { // Bach
                    this.maxIx = Math.Max(coreMeasure.maxIx, catalysts.hiIx);
                    this.minIx = Math.Min(coreMeasure.minIx, catalysts.loIx);
                } else if (coreMeasure.defined && !catalysts.defined) { // no catalysts, or Mozart
                    this.maxIx = coreMeasure.maxIx;
                    this.minIx = coreMeasure.minIx;
                } // else maxIx,minIx undefined

                if (coreMeasure.defined && catalysts.defined) { // Bach
                    this.maxDrawnIx = this.maxIx;
                    this.minDrawnIx = this.minIx;
                } else if (coreMeasure.defined && !catalysts.defined) { // no catalysts, or Mozart
                    this.maxDrawnIx = this.maxIx;
                    this.minDrawnIx = this.minIx;
                } else if (catalysts.defined && !coreMeasure.defined) { // Bach cases a->a, a+b->a+b, etc; for a->a draw it UNDER the line
                    this.maxDrawnIx = Math.Max(catalysts.loIx + 1, catalysts.hiIx); 
                    this.minDrawnIx = catalysts.loIx;
                } else { // case #->#: just draws an empty symbol half a space north of the top line by default
                    this.maxDrawnIx = 0;
                    this.minDrawnIx = -1; 
                }
            }

            public void Connect() { // compute the connections between the different elements in logical units; that's all that is needed for drawing
                this.coreMeasure.Connect(); // this will compute some useful things, but we may have to override some of them next
                if (this.catalysts.set.Count == 0) return;
                ComplexMeasure catalysts = this.catalysts;
                ComplexMeasure products = this.coreMeasure.products;
                ComplexMeasure reactants = this.coreMeasure.reactants;
                StemMeasure stem = this.coreMeasure.stem;
                if (!stem.hasStem) { // CASE a1 + .. + an -> a1 + .. + an:   i.e. reactants.set.Count == 0 && products.set.Count == 0
                    int disp = (stem.hiCatIx == stem.loCatIx) ? (stem.midCatIx == 0 ? 1 : stem.midCatIx == coreMeasure.species.Count - 1 ? -1 : 1) : 0;
                    float connect = stem.midCatIx + disp / 2.0f;
                    stem.hiDrawnIx = connect;
                    stem.loDrawnIx = connect;
                    foreach (var kvp in catalysts.set) kvp.Value.stemConnectY = connect; // catalyst.Ix + disp / 2.0f;
                    stem.hiHitIx = Math.Max(maxIx, stem.hiCatIx + disp);
                    stem.loHitIx = Math.Min(minIx, stem.loCatIx + disp);
               } else if (reactants.set.Count == 1 && products.set.Count == 0) { // CASE ci >> a -> #:   reactant.Ix = stem.hiIx = stem.loIx
                    SpeciesMeasure reactant = null; foreach (var kvp in reactants.set) reactant = kvp.Value;
                    int displ = reactant.catBound; // the orientation of attachment may have been determined earlier via catBound, but if not:
                    if (displ == 0) displ = (reactant.Ix >= stem.midCatIx) ? (reactant.Ix == 0 ? 1 : reactant.Ix == coreMeasure.species.Count - 1 ? -1 : 1) : 1;
                    float connect = reactant.Ix + displ / 2.0f;
                    stem.hiDrawnIx = connect;   
                    stem.loDrawnIx = connect;
                    reactant.stemConnectY = connect;
                    foreach (var kvp in catalysts.set) kvp.Value.stemConnectY = connect;
                    stem.hiHitIx = Math.Max(maxIx, stem.hiIx + displ);
                    stem.loHitIx = Math.Min(minIx, stem.loIx + displ);
                } else if (reactants.set.Count == 0 && products.set.Count == 1) { // CASE ci >> # -> a:   product.Ix = stem.hiIx = stem.loIx
                    SpeciesMeasure product = null; foreach (var kvp in products.set) product = kvp.Value;
                    int displ = product.catBound; // the orientation of attachment may have been determined earlier via catBound, but if not:
                    if (displ == 0) displ = (product.Ix >= stem.midCatIx) ? (product.Ix == 0 ? 1 : -1) : 1;
                    float connect = product.Ix + displ / 2.0f;
                    stem.hiDrawnIx = connect;  
                    stem.loDrawnIx = connect;
                    product.stemConnectY = connect;
                    foreach (var kvp in catalysts.set) kvp.Value.stemConnectY = connect;
                    stem.hiHitIx = Math.Max(maxIx, stem.hiIx + displ);
                    stem.loHitIx = Math.Min(minIx, stem.loIx + displ);
                } else { // we rely on stem.hiDrawnIx, stem.loDrawnIx, stem.midDrawnIx computed by Connect(measure.coreMeasure) above
                    foreach (var kvp in catalysts.set) {
                        SpeciesMeasure catalyst = kvp.Value;
                        if (stem.hiIx == stem.loIx) { // catalyst must connect to them wherever they are
                            catalyst.stemConnectY = stem.midDrawnIx;
                        } else if (catalyst.Ix <= stem.hiIx && catalyst.Ix >= stem.loIx) { // catalyst.Ix is within core stem 
                            catalyst.stemConnectY = catalyst.Ix + (catalyst.Ix <= stem.midIx ? 0.5f : -0.5f);
                        } else if (catalyst.Ix > stem.hiIx) { // catalyst.Ix is beyond stem hiIx
                            catalyst.stemConnectY = stem.hiIx - 0.5f;
                        } else if (catalyst.Ix < stem.loIx) { // catalyst.Ix is beyond stem loIx  
                            catalyst.stemConnectY = stem.loIx + 0.5f;
                        }
                    }
                    stem.hiHitIx = maxIx;
                    stem.loHitIx = minIx;
                }
                stem.midDrawnIx = ((float)(stem.hiDrawnIx + stem.loDrawnIx)) / 2.0f;
            }

            public void MeasureAndConnect() {
                StemMeasure stem = new StemMeasure(this); // compute hasStem,hiIx,loIx,midIx,hasCatStem,hiCatIx,loCatIx,midCatIx
                coreMeasure.stem = stem;
                coreMeasure.reactants.MeasureAsReactants(stem, true); // compute onStem,horSpan
                coreMeasure.products.MeasureAsProducts(stem, true); // compute onStem,horSpan
                coreMeasure.width = coreMeasure.reactants.horSpan + coreMeasure.products.horSpan;
                catalysts.MeasureAsCatalysts(coreMeasure); // compute horSpan,CatalystOnStem
                width = catalysts.defined ? coreMeasure.width + catalysts.horSpan : coreMeasure.width;

                this.Connect(); // compute connections
            }
        }
        
        public (int width, SymbolMultiset catalysts, SymbolMultiset reactants, SymbolMultiset products, ReactionMeasure measure) 
            MeasureReaction(List<SpeciesValue> species, ReactionValue reaction, string scoreStyle) {
            if (scoreStyle == "Mozart") {
                (SymbolMultiset reactants, SymbolMultiset products) = reaction.NormalForm();
                CoreReactionMeasure measure = 
                    new CoreReactionMeasure(species, 
                        new ComplexMeasure(reactants, this), 
                        new ComplexMeasure(products, this));
                measure.MeasureAndConnect();
                (SymbolMultiset hasCat, SymbolMultiset hasReac, SymbolMultiset hasProd) = reaction.CatalystForm(); // used by the influence display even in the Mozart case
                return (measure.width, hasCat, hasReac, hasProd, new ReactionMeasure(reaction, new ComplexMeasure(new SymbolMultiset(), this), measure));
            }
            else if (scoreStyle == "Bach") {
                (SymbolMultiset catalysts, SymbolMultiset reactants, SymbolMultiset products) = reaction.CatalystForm();
                ReactionMeasure measure = 
                    new ReactionMeasure(reaction,
                        new ComplexMeasure(catalysts, this), 
                        new CoreReactionMeasure(species, 
                            new ComplexMeasure(reactants, this), 
                            new ComplexMeasure(products, this)));
                measure.MeasureAndConnect();
                return (measure.width, catalysts, reactants, products, measure);
            }
            else return (0, null, null, null, null);
        }

        // ========= DRAW THE SCORE ======== //

        public SKSize OverallSize() {
            return new SKSize(
                KScore.textMargin + (this.speciesTextWidth_scaled / KScore.pinchPan.scale) + KScore.textToScoreGap + ((float)(this.scoreWidth + 2)) * Xspacing,
                ((float)(this.scoreHeight + 2)) * Yspacing);
        }

        public Swipe Rescale(Painter painter, int canvasWidth, int canvasHeight) {
            if (draggingSpeciesTag != null) return KScore.pinchPan; // don't rescale while dragging!
            // First measure everything with 1.0 scaling and no translation (translation comes only from KScoreHandler.ManualPinchPan)
            KScore.pinchPan = Swipe.Id();
            this.speciesTextWidth_scaled = RemeasureSpeciesTags(painter, painter.TextPaint(painter.font, KScore.pinchPan % KScore.textHeight, SKColors.DarkGray));
            this.scoreWidth = RemeasureReactionTags();
            // Then compute the proper scaling factor given the available canvas size
            SKSize overallSize = OverallSize();
            float xScale = ((float)canvasWidth) / overallSize.Width;
            float yScale = ((float)canvasHeight) / overallSize.Height;
            float scale = Math.Min(xScale, yScale);
            // Then compose it with the manual pinchPan, to preserve user interaction, and store it
            Swipe newPinchPan = new Swipe(scale, KScore.pinchPan.translate) + KScoreHandler.ManualPinchPan();
            KScore.pinchPan = newPinchPan;
            return newPinchPan;
        }

        // Draw, called asynchronously by the GUI
        public void Draw(Painter painter, int canvasOriginX, int canvasOriginY, int canvasWidth, int canvasHeight) {
            painter.Clear(SKColors.White);
            DrawOver(painter, canvasOriginX, canvasOriginY, canvasWidth, canvasHeight);
        }
        public void DrawOver(Painter painter, int canvasOriginX, int canvasOriginY, int canvasWidth, int canvasHeight) {
            this.canvasOriginX = canvasOriginX;
            this.canvasOriginY = canvasOriginY;
            Swipe pinchPan = Rescale(painter, canvasWidth, canvasHeight); // sets KScore.pinchPan too
            paints = new Paints(painter, pinchPan); // allocate all scaled paints
            this.speciesTextWidth_scaled = RemeasureSpeciesTags(painter, paints.speciesText);
            this.scoreOriginX = KScore.textMargin + (this.speciesTextWidth_scaled / pinchPan.scale) + KScore.textToScoreGap;

            float Ystart = canvasOriginY + scoreOriginY; // top edge of score lines, but drawing can extend above it by Yspacing
            lock (this.speciesPermutation) {
                foreach (SpeciesTag tag in this.speciesTags) tag.PaintAtY(painter, Ystart + tag.level * Yspacing, canvasOriginX, scoreOriginX, canvasWidth, pinchPan, false);
                if (draggingSpeciesTag != null) draggingSpeciesTag.PaintAtY(painter, Swipe.Inverse(draggingSpeciesTagTo, pinchPan).Y, canvasOriginX, scoreOriginX, canvasWidth, pinchPan, true);
            }
            float Xstart = canvasOriginX + scoreOriginX; // left edge of score lines   
            ReactionTag display = null;
            foreach (ReactionTag tag in this.reactionTags) {
                float X = Xstart + tag.left * Xspacing; // placement based on premeasured tag locations
                tag.ShowHilite(painter, Xstart, Ystart, paints, pinchPan);
                //if (tag.hilite) painter.DrawRoundRect(tag.HitRect(Xstart, Ystart, pinchPan), pinchPan % 4, paints.highlightFill);
                ReactionValue reaction = tag.reaction;
                if (tag.scoreStyle == "Mozart") DrawReactionMozart(tag, X, Ystart, painter, pinchPan);
                else if (tag.scoreStyle == "Bach") DrawReactionBach(tag, X, Ystart, painter, pinchPan);
                if (tag.hilite) display = tag;
            }
            if (display != null) Display(painter, display.reaction.FormatNormal(style), new SKPoint(Xstart, canvasOriginY + (0.6f* Yspacing)), paints.reactionText, paints.translucentFill, pinchPan);
            if (speciesNo > 1) {
                if (KScoreHandler.showInfluences) {
                    float X = Xstart;
                    X += Display(painter, "Feed", new SKPoint(X, canvasOriginY + (KScoreHandler.SpeciesNo() + 0.5f) * Yspacing), paints.speciesTextHiliteLegend, paints.highlightFillProduct, pinchPan);
                    X += Display(painter, "Drain", new SKPoint(X, canvasOriginY + (KScoreHandler.SpeciesNo() + 0.5f) * Yspacing), paints.speciesTextHiliteLegend, paints.highlightFillReactant, pinchPan);
                    X += Display(painter, "Catalyze", new SKPoint(X, canvasOriginY + (KScoreHandler.SpeciesNo() + 0.5f) * Yspacing), paints.speciesTextHiliteLegend, paints.highlightFillCatPlus, pinchPan);
                    X += Display(painter, "Decatalyze", new SKPoint(X, canvasOriginY + (KScoreHandler.SpeciesNo() + 0.5f) * Yspacing), paints.speciesTextHiliteLegend, paints.highlightFillCatMinus, pinchPan);
                    KScoreHandler.influenceRect = pinchPan % new SKRect(Xstart, KScoreHandler.SpeciesNo() * Yspacing, X, (KScoreHandler.SpeciesNo() + 1) * Yspacing);
                } else {
                    float X = Xstart;
                    X += Display(painter, "Show influence", new SKPoint(Xstart, canvasOriginY + (KScoreHandler.SpeciesNo() + 0.5f) * Yspacing), paints.speciesTextHiliteLegend, paints.whiteTranslucentFill, pinchPan);
                    KScoreHandler.influenceRect = pinchPan % new SKRect(Xstart, KScoreHandler.SpeciesNo() * Yspacing, X, (KScoreHandler.SpeciesNo() + 1) * Yspacing);
                }
            }
        }

        // For a general reaction, we draw the reactants on the left and the products on the right with a vertical bar in the middle
        public void DrawReactionMozart(ReactionTag tag, float leftX, float Ystart, Painter painter, Swipe pinchPan) {

            CoreReactionMeasure measure = tag.measure.coreMeasure; // to get here we must have generated Mozart coreMeasures
            float centerX = leftX + measure.reactants.horSpan * Xspacing;

            // draw the reactants
            foreach (var kvp in measure.reactants.set) {
                SpeciesMeasure reactant = kvp.Value;
                float atY = Ystart + reactant.Ix * Yspacing;
                float connectY = Ystart + reactant.stemConnectY * Yspacing;
                for (int i = 0; i < reactant.cardinality; i++) {
                    float atX = centerX - (i + 1) * Xspacing;
                    DrawReactantMozart(painter, new SKPoint(atX, atY), new SKPoint(centerX, connectY), pinchPan);
                }
            }
            
            // draw the products
            foreach (var kvp in measure.products.set) {
                SpeciesMeasure product = kvp.Value;
                float atY = Ystart + product.Ix * Yspacing;
                float connectY = Ystart + product.stemConnectY * Yspacing;
                for (int i = 0; i < product.cardinality; i++) {
                    float atX = centerX + (i + 1) * Xspacing;
                    DrawProductMozart(painter, new SKPoint(atX, atY), new SKPoint(centerX, connectY), pinchPan);
                }
            }

            // draw the stem
            float stemMinY = Ystart + measure.stem.loDrawnIx * Yspacing;
            float stemMaxY = Ystart + measure.stem.hiDrawnIx * Yspacing;
            float stemMidY = Ystart + measure.stem.midDrawnIx * Yspacing;
            if (!measure.defined) DrawEmptySymbol(painter, new SKPoint(centerX, Ystart + Yspacing / 2), 3, paints.catalystLine, pinchPan); // empty reaction
            else {
                if (measure.stem.hasStem) DrawStemMozart(painter, centerX, stemMinY, stemMaxY, paints.reactantLine, paints.productLine, pinchPan); // stem
                if (measure.reactants.set.Count == 0) DrawEmptySymbol(painter, new SKPoint(centerX, stemMidY), 3, paints.reactantLine, pinchPan);   // production
                if (measure.products.set.Count == 0) DrawEmptySymbol(painter, new SKPoint(centerX, stemMidY), 3, paints.productLine, pinchPan);  // degradation
            }
        }

        // For a catalized reaction, we draw the reactants on the left and the products on the right with a vertical bar in the middle, and the catalysts to the left of all that
        // we also straighten the first and last reactant/product along the central vertical bar, as well as catalysts in simple cases
        public void DrawReactionBach(ReactionTag tag, float leftX, float Ystart, Painter painter, Swipe pinchPan) {

            ReactionMeasure measure = tag.measure; // to get here we must have generated Bach measures
            float centerX = leftX + (measure.catalysts.horSpan + measure.coreMeasure.reactants.horSpan) * Xspacing;   // so we have computed how many columns we need on the left of centerX

            // draw the reactants
            foreach (var kvp in measure.coreMeasure.reactants.set) {
                SpeciesMeasure reactant = kvp.Value;
                float atY = Ystart + reactant.Ix * Yspacing;
                float connectY = Ystart + reactant.stemConnectY * Yspacing;
                for (int i = 0; i < reactant.cardinality; i++) {
                    int offCenterShift = i + 1; if (reactant.onStem) offCenterShift = offCenterShift - 1; // adjust reactants onStem
                    float atX = centerX - offCenterShift * Xspacing;
                    DrawReactantBach(painter, new SKPoint(atX, atY), new SKPoint(centerX, connectY), pinchPan);
                }
            }

            // draw the products
            foreach (var kvp in measure.coreMeasure.products.set) {
                SpeciesMeasure product = kvp.Value;
                float atY = Ystart + product.Ix * Yspacing;
                float connectY = Ystart + product.stemConnectY * Yspacing;
                for (int i = 0; i < product.cardinality; i++) {
                    int offCenterShift = i + 1;  if (product.onStem) offCenterShift = offCenterShift - 1; // adjust products onStem
                    float atX = centerX + offCenterShift * Xspacing;
                    DrawProductBach(painter, new SKPoint(atX, atY), new SKPoint(centerX, connectY), pinchPan);
                }
            }

            // draw the stem
            float stemMinY = Ystart + measure.coreMeasure.stem.loDrawnIx * Yspacing;
            float stemMaxY = Ystart + measure.coreMeasure.stem.hiDrawnIx * Yspacing;
            float stemMidY = Ystart + measure.coreMeasure.stem.midDrawnIx * Yspacing;
            if (!measure.defined) DrawEmptySymbol(painter, new SKPoint(centerX, Ystart + Yspacing / 2), 3, paints.catalystLine, pinchPan); // empty reaction
            else {
                if (measure.coreMeasure.stem.hasStem) DrawStemBach(painter, centerX, stemMinY, stemMaxY, paints.mixedStemLine, pinchPan); // stem
                if (measure.catalysts.set.Count == 0 && measure.coreMeasure.reactants.set.Count == 0) DrawEmptySymbol(painter, new SKPoint(centerX, stemMidY), 3, paints.reactantLine, pinchPan);   // production
                if (measure.catalysts.set.Count == 0 && measure.coreMeasure.products.set.Count == 0) DrawEmptySymbol(painter, new SKPoint(centerX, stemMidY), 3, paints.productLine, pinchPan);  // degradation
            }

            // draw the catalysts
            foreach (var kvp in measure.catalysts.set) {
                SpeciesMeasure catalyst = kvp.Value;
                float atY = Ystart + catalyst.Ix * Yspacing;
                float connectY = Ystart + catalyst.stemConnectY * Yspacing;
                for (int i = 0; i < catalyst.cardinality; i++) {
                    int offCenterShift = measure.coreMeasure.reactants.horSpan + i + 1;
                    if (measure.coreMeasure.CatalystOnStem(catalyst.Ix)) offCenterShift -= 1;  // adjust catalysts onStem
                    float atX = centerX - offCenterShift * Xspacing;
                    DrawCatalystBach(painter, new SKPoint(atX, atY), new SKPoint(centerX, connectY), i == catalyst.cardinality - 1, pinchPan);
                }
            }
        }

        // ========= RENDER ======== //

        // render connections

        public static void DrawReactantMozart(Painter painter, SKPoint at, SKPoint connect, Swipe pinchPan) {
            bool south = connect.Y >= at.Y;
            SKPoint p1 = DrawArrowTail(painter, arrowTailSize, south, at, paints.reactantFill, pinchPan);
            SKPoint p2 = DrawArrowBend(painter, p1, new SKPoint(p1.X + Xspacing, connect.Y), paints.reactantLine, pinchPan);
        }

        public static void DrawProductMozart(Painter painter, SKPoint at, SKPoint connect, Swipe pinchPan) {
            bool south = connect.Y <= at.Y;
            SKPoint p1 = DrawArrowHead(painter, arrowHeadSize, south, at, paints.productFill, pinchPan);
            SKPoint p2 = DrawArrowBend(painter, p1, new SKPoint(p1.X - Xspacing, connect.Y), paints.productLine, pinchPan);
        }

        public static void DrawStemMozart(Painter painter, float X, float Ytop, float Ybot, SKPaint reactantLinePaint, SKPaint productLinePaint, Swipe pinchPan) {
            if (Ytop == Ybot) return;
            float strokeWidth = (reactantLinePaint.StrokeWidth / pinchPan.scale) / 2; // paints already have scaling applied;
            float xr = X - strokeWidth;
            float xp = X + strokeWidth;
            painter.DrawLine(new List<SKPoint> { pinchPan % new SKPoint(xr, Ytop), pinchPan % new SKPoint(xr, Ybot) }, reactantLinePaint);
            painter.DrawLine(new List<SKPoint> { pinchPan % new SKPoint(xp, Ytop), pinchPan % new SKPoint(xp, Ybot) }, productLinePaint);
        }

        public static void DrawReactantBach(Painter painter, SKPoint at, SKPoint connect, Swipe pinchPan) {
            bool south = connect.Y >= at.Y;
            if (at.X == connect.X) {
                SKPoint p1 = DrawArrowTail(painter, arrowTailSize, south, at, paints.reactantFill, pinchPan);
                SKPoint p2 = DrawArrowTailStem(painter, south, p1, connect, paints.reactantLine, pinchPan);
            } else {
                SKPoint p1 = DrawArrowTail(painter, arrowTailSize, south, at, paints.reactantFill, pinchPan);
                SKPoint p2 = DrawArrowBend(painter, p1, new SKPoint(p1.X + Xspacing, connect.Y), paints.reactantLine, pinchPan);
                SKPoint p3 = DrawArrowTailStem(painter, south, p2, connect, paints.reactantLine, pinchPan);
            }
        }

        public static void DrawProductBach(Painter painter, SKPoint at, SKPoint connect, Swipe pinchPan) {
            bool south = connect.Y <= at.Y;
            if (at.X == connect.X) {
                SKPoint p1 = DrawArrowHead(painter, arrowHeadSize, south, at, paints.productFill, pinchPan);
                SKPoint p2 = DrawArrowHeadStem(painter, p1, connect, paints.productLine, pinchPan);
            } else {
                SKPoint p1 = DrawArrowHead(painter, arrowHeadSize, south, at, paints.productFill, pinchPan);
                SKPoint p2 = DrawArrowBend(painter, p1, new SKPoint(p1.X - Xspacing, connect.Y), paints.productLine, pinchPan);
                SKPoint p3 = DrawArrowHeadStem(painter, p2, connect, paints.productLine, pinchPan);
            }
        }

        public static void DrawCatalystBach(Painter painter, SKPoint at, SKPoint connect, bool drawHead, Swipe pinchPan) {
            bool south = connect.Y >= at.Y;
            SKPoint p1 = DrawCatalystTail(painter, arrowTailSize, south, at, paints.catalystFill, pinchPan);
            DrawArrowStemConnect(painter, south, at.Y, p1, connect, paints.catalystLine, pinchPan);
            if (drawHead) DrawCatalystHead(painter, connect, 3, paints.catalystLine, pinchPan);
        }

        public static void DrawStemBach(Painter painter, float X, float Ytop, float Ybot, SKPaint mixedLinePaint, Swipe pinchPan) {
            if (Ytop == Ybot) return;
            painter.DrawLine(new List<SKPoint> { pinchPan % new SKPoint(X, Ytop), pinchPan % new SKPoint(X, Ybot) }, mixedLinePaint);
        }

        // render connection elements

        public static SKPoint DrawArrowBend(Painter painter, SKPoint p1, SKPoint p2, SKPaint linePaint, Swipe pinchPan) {
            painter.DrawSpline(new List<SKPoint> {
                                pinchPan % p1,
                                pinchPan % new SKPoint(p1.X, p2.Y),
                                pinchPan % new SKPoint(p2.X, p2.Y),
                            }, linePaint);
            return new SKPoint(p2.X, p2.Y);
        }

        public static void DrawEmptySymbol(Painter painter, SKPoint center, float radius, SKPaint paint, Swipe pinchPan) {
            painter.DrawCircle(pinchPan % center, pinchPan % radius, paints.whiteFill);
            painter.DrawCircle(pinchPan % center, pinchPan % radius, paint);
            DrawEmptySlash(painter, center, radius, paint, pinchPan);
        }

        public static void DrawEmptySlash(Painter painter, SKPoint center, float radius, SKPaint paint, Swipe pinchPan) {
            painter.DrawLine(new List<SKPoint> {
                            pinchPan % new SKPoint(center.X + radius, center.Y - radius),
                            pinchPan % new SKPoint(center.X - radius, center.Y + radius),
            }, paint);
        }

        // input point is the center base of the tail rectangle
        // output pont is the center top of the tail rectangle
        public static SKPoint DrawArrowTail(Painter painter, float size, bool south, SKPoint point, SKPaint paint, Swipe pinchPan) {
            float lineY = south ? lineWeight / 2 : -lineWeight / 2; // lift the base of the arrow off the baseline
            float sizeY = south ? size*0.66f : -size * 0.66f;
            painter.DrawPolygon(new List<SKPoint> {
                                pinchPan % new SKPoint(point.X - size, point.Y + lineY),
                                pinchPan % new SKPoint(point.X + size, point.Y + lineY),
                                pinchPan % new SKPoint(point.X + size, point.Y + sizeY + lineY),
                                pinchPan % new SKPoint(point.X - size, point.Y + sizeY + lineY),
                            }, paint);
            return new SKPoint(point.X, point.Y + sizeY + lineY);
        }

        // input point is the tip of the arrow triangle
        // output pont is the center base of the arrow triangle
        public static SKPoint DrawArrowHead(Painter painter, float size, bool south, SKPoint point, SKPaint paint, Swipe pinchPan) {
            float lineY = south ? -lineWeight / 2 : lineWeight / 2; // lift the tip of the arrow off the baseline
            float sizeY = south ? - size : size;
            painter.DrawPolygon(new List<SKPoint> {
                                pinchPan % new SKPoint(point.X - size, point.Y + sizeY + lineY),
                                pinchPan % new SKPoint(point.X, point.Y + lineY),
                                pinchPan % new SKPoint(point.X + size, point.Y + sizeY + lineY),
                                pinchPan % new SKPoint(point.X - size, point.Y + sizeY + lineY),
                            }, paint);
            return new SKPoint(point.X, point.Y + sizeY + lineY);
        }

        public static SKPoint DrawArrowTailStem(Painter painter, bool south, SKPoint p1, SKPoint p2, SKPaint linePaint, Swipe pinchPan) {
            painter.DrawLine(new List<SKPoint> { 
                pinchPan % p1, 
                pinchPan % new SKPoint(p2.X, p2.Y), 
            }, linePaint);
            return new SKPoint(p2.X, p2.Y);
        }

        public static SKPoint DrawArrowHeadStem(Painter painter, SKPoint p1, SKPoint p2, SKPaint linePaint, Swipe pinchPan) {
            painter.DrawLine(new List<SKPoint> { 
                pinchPan % p1, 
                pinchPan % new SKPoint(p2.X, p2.Y), 
            }, linePaint);
            return new SKPoint(p2.X, p2.Y);
        }

        public static void DrawArrowStemConnect(Painter painter, bool south, float catY, SKPoint p1, SKPoint p4, SKPaint linePaint, Swipe pinchPan) {
            // catY = at.Y is the base of the stem, while p1 is the tip of the stem, and p4 = connect
            if (p1.X == p4.X) {
                painter.DrawLine(new List<SKPoint> { pinchPan % p1, pinchPan % p4 }, linePaint);
            } else {
                float bendHeight = Yspacing/2.0f - Math.Abs(catY - p1.Y);
                SKPoint p2 = new SKPoint(p1.X, p4.Y + (south? -bendHeight : bendHeight) / 2);
                SKPoint p3 = new SKPoint(p1.X + Xspacing, p4.Y);
                painter.DrawLine(new List<SKPoint> { pinchPan % p1, pinchPan % p2 }, linePaint);
                painter.DrawSpline(new List<SKPoint> {
                                    pinchPan % p2,
                                    pinchPan % new SKPoint(p2.X, p3.Y),
                                    pinchPan % p3,
                                }, linePaint);
                painter.DrawLine(new List<SKPoint> { pinchPan % p3, pinchPan % p4 }, linePaint);
            }
        }

        // input point is the center base of the tail rectangle
        // output pont is the center top of the tail rectangle
        public static SKPoint DrawCatalystTail(Painter painter, float size, bool south, SKPoint point, SKPaint fillPaint, Swipe pinchPan) {
            float lineY = south ? lineWeight / 2 : -lineWeight / 2; // lift the base of the arrow off the baseline
            float sizeY = south ? size*0.66f : -size * 0.66f;
            painter.DrawSpline(new List<SKPoint> {
                                pinchPan % new SKPoint(point.X - size, point.Y + lineY),
                                pinchPan % new SKPoint(point.X - size, point.Y + sizeY + lineY),
                                pinchPan % new SKPoint(point.X + size, point.Y + sizeY + lineY),
                                pinchPan % new SKPoint(point.X + size, point.Y + lineY),
                            }, fillPaint);
            return new SKPoint(point.X, point.Y + sizeY + lineY);
        }

        public static void DrawCatalystHead(Painter painter, SKPoint center, float radius, SKPaint paint, Swipe pinchPan) {
            painter.DrawCircle(pinchPan % center, pinchPan % radius, paints.whiteFill);
            painter.DrawCircle(pinchPan % center, pinchPan % radius, paint);
        }

    }

    // ========= INTERACTION TAGS ======== //

    public class SpeciesTag {
        public SpeciesValue species;
        public int level; // number of Yspacings from origin of score
        public string format;
        public string molarity;
        public SKRect measure_scaled; // bounding-baselined rect w.r.t. the current text paint
        public bool hilite;
        public bool hiliteCat;
        public bool hiliteReac;
        public bool hiliteProd;
        public SpeciesTag (SpeciesValue species, string molarity) {
            this.species = species;
            this.level = 0;
            this.format = "";
            this.molarity = "";
            this.measure_scaled = new SKRect(0, 0, 0, 0);
            this.hilite = false;
            this.hiliteCat = false;
            this.hiliteReac = false;
            this.hiliteProd = false;
        }
        public void SetMeasures(SKRect measure_scaled) {
            this.measure_scaled = measure_scaled;
        }
        public SKRect HitRect(float canvasOriginX, float scoreOriginY, Swipe pinchPan) {
            float left = (canvasOriginX/* + textMargin*/) + measure_scaled.Left / pinchPan.scale;
            float top = (scoreOriginY + this.level * KScore.Yspacing + KScore.textHeight/3 - KScore.textHeight/2) + measure_scaled.Top / pinchPan.scale;
            float right = (canvasOriginX + KScore.textMargin) + measure_scaled.Right / pinchPan.scale;
            float bottom = (scoreOriginY + this.level * KScore.Yspacing + KScore.textHeight/3 + KScore.textHeight/2) + measure_scaled.Bottom / pinchPan.scale;
            return pinchPan % new SKRect(left, top, right, bottom);
        }
        public void PaintLine(Painter painter, SKPoint lineStart, SKPoint lineEnd, float widen, bool hot) {
            if (hiliteReac && hiliteProd) { 
                painter.DrawRect(new SKRect(lineStart.X, lineStart.Y - widen, lineEnd.X, lineEnd.Y), KScore.paints.speciesLineProduct); 
                painter.DrawRect(new SKRect(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y + widen), KScore.paints.speciesLineReactant); 
            } else if (hiliteCat && hiliteReac) { 
                painter.DrawRect(new SKRect(lineStart.X, lineStart.Y - widen, lineEnd.X, lineEnd.Y), KScore.paints.speciesLineReactant); 
                painter.DrawRect(new SKRect(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y + widen), KScore.paints.speciesLineCatalyst); 
            } else if (hiliteCat && hiliteProd) {
                painter.DrawRect(new SKRect(lineStart.X, lineStart.Y - widen, lineEnd.X, lineEnd.Y), KScore.paints.speciesLineProduct);
                painter.DrawRect(new SKRect(lineStart.X, lineStart.Y, lineEnd.X, lineEnd.Y + widen), KScore.paints.speciesLineCatalyst);
            } else if (hiliteCat) painter.DrawRect(new SKRect(lineStart.X, lineStart.Y - widen, lineEnd.X, lineEnd.Y + widen), KScore.paints.speciesLineCatalyst);
            else if (hiliteReac) painter.DrawRect(new SKRect(lineStart.X, lineStart.Y - widen, lineEnd.X, lineEnd.Y + widen), KScore.paints.speciesLineReactant);
            else if (hiliteProd) painter.DrawRect(new SKRect(lineStart.X, lineStart.Y - widen, lineEnd.X, lineEnd.Y + widen), KScore.paints.speciesLineProduct);
            else if (hilite && !hot) painter.DrawRect(new SKRect(lineStart.X, lineStart.Y - widen, lineEnd.X, lineEnd.Y + widen), KScore.paints.speciesLineHilite);
            else if (hilite && hot) painter.DrawRect(new SKRect(lineStart.X, lineStart.Y - widen, lineEnd.X, lineEnd.Y + widen), KScore.paints.speciesLineHiliteHot);
            else painter.DrawLine(new List<SKPoint> { lineStart, lineEnd }, KScore.paints.speciesLine);
        }
        public void PaintText(Painter painter, SKPoint point, Swipe pinchPan, bool hot) {
            if (hiliteReac && hiliteProd) KScore.Display(painter, format, point, KScore.paints.speciesTextMixed, KScore.paints.speciesLineProduct, KScore.paints.speciesLineReactant, pinchPan);
            else if (hiliteCat && hiliteReac) KScore.Display(painter, format, point, KScore.paints.speciesTextReactant, KScore.paints.speciesLineReactant, KScore.paints.speciesLineCatalyst, pinchPan);
            else if (hiliteCat && hiliteProd) KScore.Display(painter, format, point, KScore.paints.speciesTextProduct, KScore.paints.speciesLineProduct, KScore.paints.speciesLineCatalyst, pinchPan);
            else if (hiliteCat) KScore.Display(painter, format, point, KScore.paints.speciesTextCatalyst, KScore.paints.speciesLineCatalyst, pinchPan);
            else if (hiliteReac) KScore.Display(painter, format, point, KScore.paints.speciesTextReactant, KScore.paints.speciesLineReactant, pinchPan); 
            else if (hiliteProd) KScore.Display(painter, format, point, KScore.paints.speciesTextProduct, KScore.paints.speciesLineProduct, pinchPan);
            else if (hilite && !hot) KScore.Display(painter, format, point, KScore.paints.speciesTextHilite, KScore.paints.speciesLineHilite, pinchPan); 
            else if (hilite && hot) KScore.Display(painter, format, point, KScore.paints.speciesTextHiliteHot, KScore.paints.speciesLineHiliteHot, pinchPan); 
            else KScore.Display(painter, format, point, KScore.paints.speciesText, null, pinchPan);
        }
        public void PaintAtY(Painter painter, float Y, float canvasOriginX, float scoreOriginX, float canvasWidth, Swipe pinchPan, bool hot) {
            SKPoint lineStart = pinchPan % new SKPoint(canvasOriginX + scoreOriginX - KScore.Xspacing / 2, Y);
            SKPoint lineEnd = pinchPan % new SKPoint(canvasOriginX + canvasWidth, Y);
            lineEnd.X = canvasOriginX + canvasWidth; // pin to the right edge
            PaintLine(painter, lineStart, lineEnd, pinchPan % 2, hot);
            PaintText(painter, new SKPoint(canvasOriginX + KScore.textMargin, Y + KScore.textHeight / 3), pinchPan, hot);
        }
    }

    public class ReactionTag {
        public ReactionValue reaction;
        public List<SpeciesValue> species; // all the species of the crn
        public string scoreStyle;
        public int left; // number of Xspacings to left of tag from origin of score
        public int top; // number of Yspacings to top of tag from origin of score
        public int right; // number of Xspacings to right of tag from origin of score
        public int bottom; // number of Yspacings to bottom of tag from origin of score
        public KScore.ReactionMeasure measure;
        public SymbolMultiset hasCat;
        public SymbolMultiset hasReac;
        public SymbolMultiset hasProd;
        public bool hilite;
        public bool hiliteReac;
        public bool hiliteProd;
        public bool hiliteCatPlus;
        public bool hiliteCatMinus;
        public ReactionTag(ReactionValue reaction, List<SpeciesValue> species, string scoreStyle) {
            this.reaction = reaction;
            this.species = species;
            this.scoreStyle = scoreStyle;
            this.hilite = false;
            this.hiliteReac = false;
            this.hiliteProd = false;
            this.hiliteCatPlus = false;
            this.hiliteCatMinus = false;
        }
        public void SetMeasures(int left, int top, int right, int bottom) {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
        public SKRect HitRect(float scoreOriginX, float scoreOriginY, Swipe pinchPan) {
            return pinchPan % new SKRect(
                scoreOriginX + left * KScore.Xspacing - KScore.Xspacing / 2, 
                scoreOriginY + top * KScore.Yspacing, 
                scoreOriginX + right * KScore.Xspacing + KScore.Xspacing / 2, 
                scoreOriginY + bottom * KScore.Yspacing);
        }
        public SKRect SpanRect() {
            return new SKRect(left, top, right+1, bottom);
        }
        public void ShowHilite(Painter painter, float Xstart, float Ystart, Paints paints, Swipe pinchPan) {
            float w = pinchPan % 4;
            SKRect hitRect = this.HitRect(Xstart, Ystart, pinchPan);
            SKRect hitRect1 = SKRect.Inflate(this.HitRect(Xstart, Ystart, pinchPan), - w/2, -w/2) ;
            float oneThird = hitRect1.Left + hitRect1.Width / 3;
            float twoThird = hitRect1.Left + 2 * hitRect1.Width / 3;
            //// Actually, feed and drain are mutually exclusive, and so are catalyze and decatalyze, so we do not need to cover these cases:
            //if (this.hiliteReac && this.hiliteCatMinus && this.hiliteCatPlus && this.hiliteProd) {
            //    float oneFourth = hitRect1.Left + hitRect1.Width / 4;
            //    float twoFourth = hitRect1.Left + 2 * hitRect1.Width / 4;
            //    float threeFourth = hitRect1.Left + 3 * hitRect1.Width / 4;
            //    KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, oneFourth, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillReactant);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(oneFourth, hitRect1.Top), new SKPoint(oneFourth, hitRect1.Bottom) }, paints.scoreLine);
            //    painter.DrawRect(new SKRect(oneFourth, hitRect1.Top, twoFourth, hitRect1.Bottom), paints.highlightFillCatMinus);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(twoFourth, hitRect1.Top), new SKPoint(twoFourth, hitRect1.Bottom) }, paints.scoreLine);
            //    painter.DrawRect(new SKRect(twoFourth, hitRect1.Top, threeFourth, hitRect1.Bottom), paints.highlightFillCatPlus);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(threeFourth, hitRect1.Top), new SKPoint(threeFourth, hitRect1.Bottom) }, paints.scoreLine);
            //    KScore.DrawRoundRect(painter, new SKRect(threeFourth, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillProduct);
            //} else if (this.hiliteReac && this.hiliteCatMinus && this.hiliteCatPlus) {
            //    KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, oneThird, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillReactant);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(oneThird, hitRect1.Top), new SKPoint(oneThird, hitRect1.Bottom) }, paints.scoreLine);
            //    painter.DrawRect(new SKRect(oneThird, hitRect1.Top, twoThird, hitRect1.Bottom), paints.highlightFillCatMinus);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(twoThird, hitRect1.Top), new SKPoint(twoThird, hitRect1.Bottom) }, paints.scoreLine);
            //    KScore.DrawRoundRect(painter, new SKRect(twoThird, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillCatPlus);
            //} else if (this.hiliteReac && this.hiliteCatMinus && this.hiliteProd) {
            //    KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, oneThird, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillReactant);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(oneThird, hitRect1.Top), new SKPoint(oneThird, hitRect1.Bottom) }, paints.scoreLine);
            //    painter.DrawRect(new SKRect(oneThird, hitRect1.Top, twoThird, hitRect1.Bottom), paints.highlightFillCatMinus);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(twoThird, hitRect1.Top), new SKPoint(twoThird, hitRect1.Bottom) }, paints.scoreLine);
            //    KScore.DrawRoundRect(painter, new SKRect(twoThird, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillProduct);
            //} else if (this.hiliteReac && this.hiliteCatPlus && this.hiliteProd) {
            //    KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, oneThird, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillReactant);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(oneThird, hitRect1.Top), new SKPoint(oneThird, hitRect1.Bottom) }, paints.scoreLine);
            //    painter.DrawRect(new SKRect(oneThird, hitRect1.Top, twoThird, hitRect1.Bottom), paints.highlightFillCatPlus);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(twoThird, hitRect1.Top), new SKPoint(twoThird, hitRect1.Bottom) }, paints.scoreLine);
            //    KScore.DrawRoundRect(painter, new SKRect(twoThird, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillProduct);
            //} else if (this.hiliteCatMinus && this.hiliteCatPlus && this.hiliteProd) {
            //    KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, oneThird, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillCatMinus);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(oneThird, hitRect1.Top), new SKPoint(oneThird, hitRect1.Bottom) }, paints.scoreLine);
            //    painter.DrawRect(new SKRect(oneThird, hitRect1.Top, twoThird, hitRect1.Bottom), paints.highlightFillCatPlus);
            //    painter.DrawLine(new List<SKPoint> { new SKPoint(twoThird, hitRect1.Top), new SKPoint(twoThird, hitRect1.Bottom) }, paints.scoreLine);
            //    KScore.DrawRoundRect(painter, new SKRect(twoThird, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillProduct);
            //} else 
            if (this.hiliteReac && this.hiliteCatMinus) {
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, hitRect1.MidX, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillReactant);
                painter.DrawLine(new List<SKPoint> { new SKPoint(hitRect1.MidX, hitRect1.Top), new SKPoint(hitRect1.MidX, hitRect1.Bottom) }, paints.scoreLine);
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.MidX, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillCatMinus);
            } else if (this.hiliteReac && this.hiliteCatPlus) {
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, hitRect1.MidX, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillReactant);
                painter.DrawLine(new List<SKPoint> { new SKPoint(hitRect1.MidX, hitRect1.Top), new SKPoint(hitRect1.MidX, hitRect1.Bottom) }, paints.scoreLine);
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.MidX, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillCatPlus);
            } else if (this.hiliteReac && this.hiliteProd) {
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, hitRect1.MidX, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillReactant);
                painter.DrawLine(new List<SKPoint> { new SKPoint(hitRect1.MidX, hitRect1.Top), new SKPoint(hitRect1.MidX, hitRect1.Bottom) }, paints.scoreLine);
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.MidX, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillProduct);
            } else if (this.hiliteCatMinus && this.hiliteCatPlus) {
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, hitRect1.MidX, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillCatMinus);
                painter.DrawLine(new List<SKPoint> { new SKPoint(hitRect1.MidX, hitRect1.Top), new SKPoint(hitRect1.MidX, hitRect1.Bottom) }, paints.scoreLine);
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.MidX, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillCatPlus);
            } else if (this.hiliteCatMinus && this.hiliteProd) {
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, hitRect1.MidX, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillCatMinus);
                painter.DrawLine(new List<SKPoint> { new SKPoint(hitRect1.MidX, hitRect1.Top), new SKPoint(hitRect1.MidX, hitRect1.Bottom) }, paints.scoreLine);
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.MidX, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillProduct);
            } else if (this.hiliteCatPlus && this.hiliteProd) {
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.Left, hitRect1.Top, hitRect1.MidX, hitRect1.Bottom), w, 0, w, 0, paints.highlightFillCatPlus);
                painter.DrawLine(new List<SKPoint> { new SKPoint(hitRect1.MidX, hitRect1.Top), new SKPoint(hitRect1.MidX, hitRect1.Bottom) }, paints.scoreLine);
                KScore.DrawRoundRect(painter, new SKRect(hitRect1.MidX, hitRect1.Top, hitRect1.Right, hitRect1.Bottom), 0, w, 0, w, paints.highlightFillProduct);
            } else if (this.hiliteReac) painter.DrawRoundRect(hitRect1, w, paints.highlightFillReactant);
            else if (this.hiliteProd) painter.DrawRoundRect(hitRect1, w, paints.highlightFillProduct);
            else if (this.hiliteCatPlus) painter.DrawRoundRect(hitRect1, w, paints.highlightFillCatPlus);
            else if (this.hiliteCatMinus) painter.DrawRoundRect(hitRect1, w, paints.highlightFillCatMinus);

            if (this.hilite) { if (KScoreHandler.showInfluences) painter.DrawRoundRect(hitRect, w, paints.highlightStrike); else painter.DrawRoundRect(hitRect, w, paints.highlightFill);
            }
        }
    }

    // ========= PAINTS ======== //

    public class Paints {
        public SKPaint reactionText;
        public SKPaint speciesText;
        public SKPaint speciesTextReactant;
        public SKPaint speciesTextProduct;
        public SKPaint speciesTextMixed;
        public SKPaint speciesTextCatalyst;
        public SKPaint speciesTextHilite;
        public SKPaint speciesTextHiliteHot;
        public SKPaint speciesTextHiliteLegend;
        public SKPaint speciesLine;
        public SKPaint speciesLineReactant;
        public SKPaint speciesLineProduct;
        public SKPaint speciesLineMixed;
        public SKPaint speciesLineCatalyst;
        public SKPaint speciesLineHilite;
        public SKPaint speciesLineHiliteHot;
        public SKPaint reactantFill;
        public SKPaint reactantLine;
        public SKPaint reactantStemLine;
        public SKPaint productFill;
        public SKPaint productLine;
        public SKPaint productStemLine;
        public SKPaint mixedStemLine;
        public SKPaint catalystFill;
        public SKPaint catalystLine;
        public SKPaint whiteFill;
        public SKPaint whiteTranslucentFill;
        public SKPaint whiteLine;
        public SKPaint highlightFill;
        public SKPaint highlightStrike;
        public SKPaint highlightFillReactant;
        public SKPaint highlightFillProduct;
        public SKPaint highlightFillCatPlus;
        public SKPaint highlightFillCatMinus;
        public SKPaint scoreLine;
        public SKPaint translucentFill;
        public SKPaint yellowFill;
        public SKPaint debugFill;
        public SKPaint debugLine;
        public Paints(Painter painter, Swipe pinchPan) {
            SKColor Red =  SKColors.Red; 
            SKColor Green = SKColors.Green;
            SKColor Blue = SKColors.Blue; 
            SKColor Yellow = SKColors.Yellow;
            SKColor DarkYellow = SKColors.Gold;
            SKColor Orange = SKColors.Orange;
            SKColor Purple = SKColors.Purple;
            SKColor White = SKColors.White; //new SKColor(255,255,255,255);
            reactionText = painter.TextPaint(painter.font, pinchPan % KScore.textHeight, SKColors.DarkSlateBlue);
            speciesText = painter.TextPaint(painter.font, pinchPan % KScore.textHeight, SKColors.DarkGray);
            speciesTextReactant = painter.TextPaint(painter.font, pinchPan % KScore.textHeight, Blue);
            speciesTextProduct = painter.TextPaint(painter.font, pinchPan % KScore.textHeight, Red);
            speciesTextCatalyst = painter.TextPaint(painter.font, pinchPan % KScore.textHeight, Green);
            speciesTextMixed = painter.TextPaint(painter.font, pinchPan % KScore.textHeight, Purple);
            speciesTextHilite = painter.TextPaint(painter.font, pinchPan % KScore.textHeight, Orange);
            speciesTextHiliteHot = painter.TextPaint(painter.font, pinchPan % KScore.textHeight, Purple);
            speciesTextHiliteLegend = painter.TextPaint(painter.font, (pinchPan % KScore.textHeight/3), SKColors.DarkGray);
            speciesLine = painter.LinePaint(pinchPan % KScore.lineWeight, SKColors.DarkGray, SKStrokeCap.Round);
            speciesLineReactant = painter.FillPaint(new SKColor(Blue.Red, Blue.Green, Blue.Blue, 63));
            speciesLineProduct = painter.FillPaint(new SKColor(Red.Red, Red.Green, Red.Blue, 63));
            speciesLineMixed = painter.FillPaint(new SKColor(Purple.Red, Purple.Green, Purple.Blue, 63));
            speciesLineCatalyst = painter.FillPaint(new SKColor(Green.Red, Green.Green, Green.Blue, 63));
            speciesLineHilite = painter.FillPaint(new SKColor(Orange.Red, Orange.Green, Orange.Blue, 63));
            speciesLineHiliteHot = painter.FillPaint(new SKColor(Purple.Red, Purple.Green, Purple.Blue, 63));
            reactantFill = painter.FillPaint(Blue);
            reactantLine = painter.LinePaint(pinchPan % KScore.lineWeight, Blue, SKStrokeCap.Round);
            reactantStemLine = painter.LinePaint(pinchPan % KScore.lineWeight /2, Blue, SKStrokeCap.Round);
            productFill = painter.FillPaint(Red);
            productLine = painter.LinePaint(pinchPan % KScore.lineWeight, Red, SKStrokeCap.Round);
            productStemLine = painter.LinePaint(pinchPan % KScore.lineWeight /2, Red, SKStrokeCap.Round);
            mixedStemLine = painter.LinePaint(pinchPan % KScore.lineWeight, Purple, SKStrokeCap.Butt);
            catalystFill = painter.FillPaint(Green);
            catalystLine = painter.LinePaint(pinchPan % KScore.lineWeight, Green, SKStrokeCap.Round);
            whiteFill = painter.FillPaint(White);
            whiteTranslucentFill = painter.FillPaint(new SKColor(255, 255, 255, 100));
            whiteLine = painter.LinePaint(pinchPan % KScore.lineWeight, White, SKStrokeCap.Round);
            highlightFill = painter.FillPaint(new SKColor(255, 127, 0, 127));
            highlightStrike = painter.LinePaint(pinchPan % KScore.lineWeight, new SKColor(255, 127, 0, 127));
            highlightFillReactant = painter.FillPaint(new SKColor(Blue.Red, Blue.Green, Blue.Blue, 31));
            highlightFillProduct = painter.FillPaint(new SKColor(Red.Red, Red.Green, Red.Blue, 31));
            highlightFillCatPlus = painter.FillPaint(new SKColor(Green.Red, Green.Green, Green.Blue, 31));
            highlightFillCatMinus = painter.FillPaint(new SKColor(127, 63, 63, 31));
            scoreLine = painter.LinePaint(pinchPan % KScore.lineWeight/4, new SKColor(127,127,127,127), SKStrokeCap.Butt);
            yellowFill = painter.FillPaint(new SKColor(255, 255, 192, 255));
            translucentFill = painter.FillPaint(new SKColor(255, 127, 0, 100));
            debugFill = painter.FillPaint(Yellow);
            debugLine = painter.LinePaint(pinchPan % KScore.lineWeight, Yellow, SKStrokeCap.Round);
        }
    }

}

