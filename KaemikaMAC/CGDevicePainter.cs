using System;
using SkiaSharp;
using CoreGraphics;
using Kaemika;

namespace KaemikaMAC
{
    public class CGDevicePainter : CGPainter, DevicePainter
    {
        public static CGBitmapContext cachedBackground = null; // keep this field public and static or it will apparently be garbage collected and set to null after every redraw !!!!

        public CGDevicePainter(CGContext canvas) : base(canvas)
        {
        }

        public /*interface DevicePainter*/ void Draw(ProtocolDevice.Device device, int canvasX, int canvasY, int canvasWidth, int canvasHeight)
        {

            (float margin, float padRadius, float deviceWidth, float deviceHeight) = device.FittedDimensions(canvasWidth, canvasHeight);
            float strokeWidth = padRadius / 10.0f;
            float accentStrokeWidth = 1.5f * strokeWidth;
            float textStrokeWidth = accentStrokeWidth / 2.0f;
            float deviceX = canvasX + (canvasWidth - deviceWidth) / 2.0f;
            float deviceY = canvasY + (canvasHeight - deviceHeight) / 2.0f;

            // don't lock device here, it will dedlock
            if (device.sizeChanged || cachedBackground == null || cachedBackground.Width != canvasWidth || cachedBackground.Height != canvasHeight)
            {
                cachedBackground = CG.Bitmap(canvasWidth, canvasHeight);
                DrawDevice(device, cachedBackground,
                    canvasX, canvasY, canvasWidth, canvasHeight,
                    deviceX, deviceY, deviceWidth, deviceHeight,
                    padRadius, margin, device.pinchPan);
                device.sizeChanged = false;
            }

            if (!device.sizeChanged)
            {
                // copy cachedBackground to canvas DOH!
                canvas.AsBitmapContext().DrawImage(new CGRect(0, 0, cachedBackground.Width, cachedBackground.Height), cachedBackground.ToImage());
                // do not apply pinchPan: background bitmap is alread scaled by it
            }

            if (device.displayPinchOrigin)
            {
                // same as: GraphSharp.GraphLayout.CanvasDrawCircle(canvas, pinchOrigin, 20, false, SKColors.LightGray);
                // same as: using (var paint = FillPaint(SKColors.LightGray)) { painter.DrawCircle(pinchOrigin, 20, paint); }
                //using (var paint = new SKPaint()) {
                //    paint.TextSize = 10; paint.IsAntialias = true; paint.Color = SKColors.LightGray; paint.IsStroke = false;
                //    canvas.DrawCircle(device.pinchOrigin.X, device.pinchOrigin.Y, 20, paint);
                //}
            }

            using (var dropletFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = device.dropletColor, IsAntialias = true })
            using (var dropletBiggieFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = device.dropletBiggieColor, IsAntialias = true })
            {
                ProtocolDevice.Place[,] places = device.places;
                ProtocolDevice.Placement placement = device.placement;
                for (int row = 0; row < places.GetLength(0); row++)
                {
                    for (int col = 0; col < places.GetLength(1); col++)
                    {
                        ProtocolDevice.Place place = places[row, col];
                        if (place != null && placement.IsOccupied(place))
                        {
                            SampleValue sample = placement.SampleOf(place);
                            float volumeRadius = padRadius * (float)Math.Sqrt((sample.Volume()) * 1000000.0); // normal radius = 1μL
                            float diameter = 2 * padRadius;
                            SKPaint fillPaint = dropletFillPaint;
                            bool biggie = false;
                            if (volumeRadius > 2 * padRadius)
                            {
                                biggie = true;
                                volumeRadius = 2 * padRadius;
                                fillPaint = dropletBiggieFillPaint;
                            }
                            SKPoint here = new SKPoint(deviceX + margin + padRadius + col * diameter, deviceY + margin + padRadius + row * diameter);
                            SKPoint rht = new SKPoint(deviceX + margin + padRadius + (col + 1) * diameter, deviceY + margin + padRadius + row * diameter);
                            SKPoint lft = new SKPoint(deviceX + margin + padRadius + (col - 1) * diameter, deviceY + margin + padRadius + row * diameter);
                            SKPoint bot = new SKPoint(deviceX + margin + padRadius + col * diameter, deviceY + margin + padRadius + (row + 1) * diameter);
                            SKPoint top = new SKPoint(deviceX + margin + padRadius + col * diameter, deviceY + margin + padRadius + (row - 1) * diameter);
                            string label = sample.symbol.Raw(); // sample.FormatSymbol(placement.StyleOf(sample, style))
                            if (place.IsAnimation(ProtocolDevice.Animation.None))
                                DrawDroplet(canvas, label, biggie, here,
                                    padRadius, volumeRadius, textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SizeHalf))
                                DrawDroplet(canvas, label, biggie, here,
                                    padRadius, volumeRadius / 2, textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SizeQuarter))
                                DrawDroplet(canvas, label, biggie, here,
                                    padRadius, volumeRadius / 4, textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.PullRht))
                                DrawDropletPulledHor(canvas, label, biggie, here, rht, ProtocolDevice.Direction.Rht,
                                    padRadius, volumeRadius * 5 / 6, volumeRadius * 5 / 12, volumeRadius * 1 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SplitRht))
                                DrawDropletPulledHor(canvas, label, biggie, here, rht, ProtocolDevice.Direction.Rht,
                                    padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.PullLft))
                                DrawDropletPulledHor(canvas, label, biggie, lft, here, ProtocolDevice.Direction.Lft,
                                    padRadius, volumeRadius * 1 / 3, volumeRadius * 5 / 12, volumeRadius * 5 / 6,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SplitLft))
                                DrawDropletPulledHor(canvas, label, biggie, lft, here, ProtocolDevice.Direction.Lft,
                                    padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.PullBot))
                                DrawDropletPulledVer(canvas, label, biggie, here, bot, ProtocolDevice.Direction.Bot,
                                    padRadius, volumeRadius * 5 / 6, volumeRadius * 5 / 12, volumeRadius * 1 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SplitBot))
                                DrawDropletPulledVer(canvas, label, biggie, here, bot, ProtocolDevice.Direction.Bot,
                                    padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.PullTop))
                                DrawDropletPulledVer(canvas, label, biggie, top, here, ProtocolDevice.Direction.Top,
                                    padRadius, volumeRadius * 1 / 3, volumeRadius * 5 / 12, volumeRadius * 5 / 6,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SplitTop))
                                DrawDropletPulledVer(canvas, label, biggie, top, here, ProtocolDevice.Direction.Top,
                                    padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                        }
                    }
                }
            }

            canvas.Flush();
        }

        public static SKColor coldColor = new SKColor(0, 127, 127, 63);
        public static SKColor hotColor = new SKColor(127, 0, 127, 63);

        public SKRect InflateRect(SKRect rect, float n)
        {
            SKRect inflated = rect;
            inflated.Inflate(n, n);
            return inflated;
        }

        public void DrawBackground(CGContext canvas, float canvasX, float canvasY, float canvasWidth, float canvasHeight)
        {
            var backPaint = CG.Color(ProtocolDevice.deviceBackColor);
            canvas.SetFillColor(backPaint);
            var path = new CGPath();
            path.AddRect(new CGRect(canvasX, canvasY, canvasWidth, canvasHeight));
            canvas.AddPath(path);
            canvas.FillPath();
        }

        public void DrawDevice(ProtocolDevice.Device device, CGContext canvas,
            float canvasX, float canvasY, float canvasWidth, float canvasHeight,
            float deviceX, float deviceY, float deviceWidth, float deviceHeight,
            float padRadius, float margin, Swipe swipe)
        {

            DrawBackground(canvas, canvasX, canvasY, canvasWidth, canvasHeight);

            float padStrokeWidth = padRadius / 10.0f;
            float padStrokeWidthAccent = 0.75f * padStrokeWidth;

            SKRect coldZoneRect = new SKRect(deviceX + margin, deviceY + margin, deviceX + margin + device.coldZoneWidth * 2 * padRadius, deviceY + margin + device.rowsNo * 2 * padRadius);
            SKRect hotZoneRect = new SKRect(deviceX + margin + (device.coldZoneWidth + device.warmZoneWidth) * 2 * padRadius, deviceY + margin, deviceX + margin + (device.coldZoneWidth + device.warmZoneWidth + device.hotZoneWidth) * 2 * padRadius, deviceY + margin + device.rowsNo * 2 * padRadius);

            DrawHeatZone(canvas, coldZoneRect, padRadius, coldZoneRect.Width / (2 * device.coldZoneWidth), coldColor, swipe);
            DrawHeatZone(canvas, hotZoneRect, padRadius, hotZoneRect.Width / (2 * device.hotZoneWidth), hotColor, swipe);
            using (var zoneTextPaint = new SKPaint { Style = SKPaintStyle.Fill, TextSize = swipe % padRadius, TextAlign = SKTextAlign.Center, Color = SKColors.Blue, IsAntialias = true })
            {
                CG.DrawTextS(canvas, "< " + ProtocolDevice.coldTemp, swipe % new SKPoint(coldZoneRect.MidX, coldZoneRect.Bottom + margin), zoneTextPaint);
                CG.DrawTextS(canvas, "> " + ProtocolDevice.hotTemp, swipe % new SKPoint(hotZoneRect.MidX, hotZoneRect.Bottom + margin), zoneTextPaint);
            }

            float strokePaintStrokeWidth = padStrokeWidth;
            float strokeAccentPaintStrokeWidth = padStrokeWidthAccent;

            using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.Black, StrokeWidth = swipe % strokePaintStrokeWidth, StrokeJoin = SKStrokeJoin.Round, IsAntialias = true })
            using (var strokeAccentPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.White, StrokeWidth = swipe % strokeAccentPaintStrokeWidth, StrokeJoin = SKStrokeJoin.Round, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            using (var fillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Goldenrod, IsAntialias = true }) // SKColors.DarkGoldenrod / Goldenrod / PaleGoldenrod / LightGoldenrodYellow / Gold
            using (var holePaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Black, IsAntialias = true })
            using (var holeAccentPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.White, IsAntialias = true })
            {
                for (int j = 0; j < device.rowsNo; j++) for (int i = 0; i < device.colsNo; i++)
                        DrawPad(canvas, new SKPoint(deviceX + margin + padRadius + i * 2 * padRadius, deviceY + margin + padRadius + j * 2 * padRadius), padRadius, strokePaint, strokePaintStrokeWidth, fillPaint, holePaint, strokeAccentPaint, strokeAccentPaintStrokeWidth, holeAccentPaint, swipe);
            }
        }

        public void DrawHeatZone(CGContext canvas, SKRect zone, float halo, float groove, SKColor color, Swipe swipe)
        {
            var radialGradient = SKShader.CreateRadialGradient(swipe % new SKPoint(zone.MidX, zone.MidY), swipe % groove, new SKColor[2] { color, ProtocolDevice.deviceBackColor }, null, SKShaderTileMode.Mirror); //, scaleMatrix);
            using (var gradientPaint = new SKPaint { Style = SKPaintStyle.Fill, Shader = radialGradient })
            {
                var path = new CGPath();
                path.AddRoundedRect(CG.Rect(swipe % InflateRect(zone, halo)), swipe % halo, swipe % halo);
                canvas.AddPath(path);
                canvas.SetFillColor(CG.Color(color));
                canvas.FillPath();
            }
        }

        public void DrawDropletLabel(CGContext canvas, string label, SKPoint center, float radius, float strokeWidth, bool biggie, Swipe swipe)
        {
            using (var labelPaint = new SKPaint { Style = SKPaintStyle.Stroke, TextSize = swipe % (0.5f * radius), TextAlign = SKTextAlign.Center, Color = new SKColor(255, 255, 255, 191), StrokeWidth = swipe % strokeWidth, IsAntialias = true })
            {
                CG.DrawTextS(canvas, label, swipe % new SKPoint(center.X, center.Y + 0.1f * radius), labelPaint);
                if (biggie) CG.DrawTextS(canvas, ">4μL", swipe % new SKPoint(center.X, center.Y + 0.6f * radius), labelPaint);
            }
        }

        public void DrawDroplet(CGContext canvas, string label, bool biggie, SKPoint center, float padRadius, float radius, float textStrokeWidth, SKPaint fillPaint, float strokeWidth, float accentStrokeWidth, Swipe swipe)
        {
            float ratio = radius / padRadius;
            //strokeWidth = strokeWidth * ratio;
            textStrokeWidth = textStrokeWidth * ratio;
            accentStrokeWidth = accentStrokeWidth * ratio;

            float pull = 0.75f;
            float accentRadius = radius - (strokeWidth + accentStrokeWidth) / 2.0f;

            var path = new CGPath();
            path.MoveToPoint(CG.Point(swipe % new SKPoint(center.X, center.Y - radius)));
            // The parameters to CGPath.AddCurveToPoint are in the same order as in SKPath.CubicTo (despite what the Apple docs seem to say).
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(center.X + pull * radius, center.Y - radius)), CG.Point(swipe % new SKPoint(center.X + radius, center.Y - pull * radius)), CG.Point(swipe % new SKPoint(center.X + radius, center.Y)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(center.X + radius, center.Y + pull * radius)), CG.Point(swipe % new SKPoint(center.X + pull * radius, center.Y + radius)), CG.Point(swipe % new SKPoint(center.X, center.Y + radius)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(center.X - pull * radius, center.Y + radius)), CG.Point(swipe % new SKPoint(center.X - radius, center.Y + pull * radius)), CG.Point(swipe % new SKPoint(center.X - radius, center.Y)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(center.X - radius, center.Y - pull * radius)), CG.Point(swipe % new SKPoint(center.X - pull * radius, center.Y - radius)), CG.Point(swipe % new SKPoint(center.X, center.Y - radius)));
            path.CloseSubpath();

            var darkPath = new CGPath();
            darkPath.MoveToPoint(CG.Point(swipe % new SKPoint(center.X + accentRadius, center.Y)));
            darkPath.AddCurveToPoint(CG.Point(swipe % new SKPoint(center.X + accentRadius, center.Y + pull * accentRadius)), CG.Point(swipe % new SKPoint(center.X + pull * accentRadius, center.Y + accentRadius)), CG.Point(swipe % new SKPoint(center.X, center.Y + accentRadius)));

            var lightPath = new CGPath();
            lightPath.MoveToPoint(CG.Point(swipe % new SKPoint(center.X - accentRadius, center.Y)));
            lightPath.AddCurveToPoint(CG.Point(swipe % new SKPoint(center.X - accentRadius, center.Y - pull * accentRadius)), CG.Point(swipe % new SKPoint(center.X - pull * accentRadius, center.Y - accentRadius)), CG.Point(swipe % new SKPoint(center.X, center.Y - accentRadius)));

            using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 191), StrokeWidth = swipe % strokeWidth, IsAntialias = true })
            using (var accentLightPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(255, 255, 255, 191), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            using (var accentDarkPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 95), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            {

                canvas.AddPath(path);
                canvas.SetFillColor(CG.Color(fillPaint.Color));
                canvas.FillPath();

                canvas.AddPath(darkPath);
                canvas.SetStrokeColor(CG.Color(accentDarkPaint.Color));
                canvas.SetLineWidth(accentDarkPaint.StrokeWidth);
                canvas.SetLineJoin(CG.LineJoin(accentDarkPaint.StrokeJoin));
                canvas.SetLineCap(CG.LineCap(accentDarkPaint.StrokeCap));
                canvas.StrokePath();

                canvas.AddPath(lightPath);
                canvas.SetStrokeColor(CG.Color(accentLightPaint.Color));
                canvas.SetLineWidth(accentLightPaint.StrokeWidth);
                canvas.SetLineJoin(CG.LineJoin(accentLightPaint.StrokeJoin));
                canvas.SetLineCap(CG.LineCap(accentLightPaint.StrokeCap));
                canvas.StrokePath();

                canvas.AddPath(path);
                canvas.SetStrokeColor(CG.Color(strokePaint.Color));
                canvas.SetLineWidth(strokePaint.StrokeWidth);
                canvas.SetLineJoin(CG.LineJoin(strokePaint.StrokeJoin));
                canvas.SetLineCap(CG.LineCap(strokePaint.StrokeCap));
                canvas.StrokePath();
            }

            DrawDropletLabel(canvas, label, center, radius, textStrokeWidth, biggie, swipe);
        }

        public void DrawDropletPulledHor(CGContext canvas, string label, bool biggie, SKPoint c1, SKPoint c2, ProtocolDevice.Direction dir, float r, float r1, float neckY, float r2, float textStrokeWidth, SKPaint fillPaint, float strokeWidth, float accentStrokeWidth, Swipe swipe)
        {
            float ratio = ((r1 + r2) / 2) / r;
            //strokeWidth = strokeWidth * ratio;
            textStrokeWidth = textStrokeWidth * ratio;
            accentStrokeWidth = accentStrokeWidth * ratio;

            float lr = r1 - (strokeWidth + accentStrokeWidth) / 2.0f;
            float dr = r2 - (strokeWidth + accentStrokeWidth) / 2.0f;

            // place the neck in an X-position proportional to the rate between r1 and r2, with m1+r1X to the left leading to c1.X, and m2+r2X to the right leading to c2.X
            float m1 = (2 * r - (r1 + r2)) / (1 + r2 / r1);
            float m2 = m1 * r2 / r1;
            // position of the neck
            float nX = c1.X + r1 + m1;
            float nY = c1.Y;
            // Control points: a1*2 is on a line from nX,nY-neckY to c1.X,c.Y-r1 where it intersects c1.X+r1; we divide it by 2 to make the curve smoother
            float a1 = (m1 * (r1 - neckY) / (r1 + m1)) / 2;
            float a2 = (m2 * (r2 - neckY) / (r2 + m2)) / 2;

            var path = new CGPath();
            path.MoveToPoint(CG.Point(swipe % new SKPoint(c1.X, c1.Y - r1)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c1.X + 0.5f * r1, c1.Y - r1)), CG.Point(swipe % new SKPoint(c1.X + r1, nY - (neckY + a1))), CG.Point(swipe % new SKPoint(nX, nY - neckY)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c2.X - r2, nY - (neckY + a2))), CG.Point(swipe % new SKPoint(c2.X - 0.5f * r2, c2.Y - r2)), CG.Point(swipe % new SKPoint(c2.X, c2.Y - r2)));

            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c2.X + 0.75f * r2, c2.Y - r2)), CG.Point(swipe % new SKPoint(c2.X + r2, c2.Y - 0.75f * r2)), CG.Point(swipe % new SKPoint(c2.X + r2, c2.Y)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c2.X + r2, c2.Y + 0.75f * r2)), CG.Point(swipe % new SKPoint(c2.X + 0.75f * r2, c2.Y + r2)), CG.Point(swipe % new SKPoint(c2.X, c2.Y + r2)));

            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c2.X - 0.5f * r2, c2.Y + r2)), CG.Point(swipe % new SKPoint(c2.X - r2, nY + (neckY + a2))), CG.Point(swipe % new SKPoint(nX, nY + neckY)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c1.X + r1, nY + (neckY + a1))), CG.Point(swipe % new SKPoint(c1.X + 0.5f * r1, c1.Y + r1)), CG.Point(swipe % new SKPoint(c1.X, c1.Y + r1)));

            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c1.X - 0.75f * r1, c1.Y + r1)), CG.Point(swipe % new SKPoint(c1.X - r1, c1.Y + 0.75f * r1)), CG.Point(swipe % new SKPoint(c1.X - r1, c1.Y)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c1.X - r1, c1.Y - 0.75f * r1)), CG.Point(swipe % new SKPoint(c1.X - 0.75f * r1, c1.Y - r1)), CG.Point(swipe % new SKPoint(c1.X, c1.Y - r1)));
            path.CloseSubpath();

            var darkPath = new CGPath();
            darkPath.MoveToPoint(CG.Point(swipe % new SKPoint(c2.X + dr, c2.Y)));
            darkPath.AddCurveToPoint(CG.Point(swipe % new SKPoint(c2.X + dr, c2.Y + 0.75f * dr)), CG.Point(swipe % new SKPoint(c2.X + 0.75f * dr, c2.Y + dr)), CG.Point(swipe % new SKPoint(c2.X, c2.Y + dr)));

            var lightPath = new CGPath();
            lightPath.MoveToPoint(CG.Point(swipe % new SKPoint(c1.X - lr, c1.Y)));
            lightPath.AddCurveToPoint(CG.Point(swipe % new SKPoint(c1.X - lr, c1.Y - 0.75f * lr)), CG.Point(swipe % new SKPoint(c1.X - 0.75f * lr, c1.Y - lr)), CG.Point(swipe % new SKPoint(c1.X, c1.Y - lr)));

            using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 191), StrokeWidth = swipe % strokeWidth, IsAntialias = true })
            using (var accentLightPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(255, 255, 255, 191), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            using (var accentDarkPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 95), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            {

                canvas.AddPath(path);
                canvas.SetFillColor(CG.Color(fillPaint.Color));
                canvas.FillPath();

                canvas.AddPath(darkPath);
                canvas.SetStrokeColor(CG.Color(accentDarkPaint.Color));
                canvas.SetLineWidth(accentDarkPaint.StrokeWidth);
                canvas.SetLineJoin(CG.LineJoin(accentDarkPaint.StrokeJoin));
                canvas.SetLineCap(CG.LineCap(accentDarkPaint.StrokeCap));
                canvas.StrokePath();

                canvas.AddPath(lightPath);
                canvas.SetStrokeColor(CG.Color(accentLightPaint.Color));
                canvas.SetLineWidth(accentLightPaint.StrokeWidth);
                canvas.SetLineJoin(CG.LineJoin(accentLightPaint.StrokeJoin));
                canvas.SetLineCap(CG.LineCap(accentLightPaint.StrokeCap));
                canvas.StrokePath();

                canvas.AddPath(path);
                canvas.SetStrokeColor(CG.Color(strokePaint.Color));
                canvas.SetLineWidth(strokePaint.StrokeWidth);
                canvas.SetLineJoin(CG.LineJoin(strokePaint.StrokeJoin));
                canvas.SetLineCap(CG.LineCap(strokePaint.StrokeCap));
                canvas.StrokePath();
            }

            float rText = (r1 == r2) ? ((dir == ProtocolDevice.Direction.Lft) ? r1 : r2) : (r1 > r2) ? r1 : r2;
            SKPoint cText = (r1 == r2) ? ((dir == ProtocolDevice.Direction.Lft) ? c1 : c2) : (r1 > r2) ? c1 : c2;
            DrawDropletLabel(canvas, label, cText, rText, textStrokeWidth, biggie, swipe);
        }

        public void DrawDropletPulledVer(CGContext canvas, string label, bool biggie, SKPoint c1, SKPoint c2, ProtocolDevice.Direction dir, float r, float r1, float neckX, float r2, float textStrokeWidth, SKPaint fillPaint, float strokeWidth, float accentStrokeWidth, Swipe swipe)
        {
            float ratio = ((r1 + r2) / 2) / r;
            //strokeWidth = strokeWidth * ratio;
            textStrokeWidth = textStrokeWidth * ratio;
            accentStrokeWidth = accentStrokeWidth * ratio;

            float lr = r1 - (strokeWidth + accentStrokeWidth) / 2.0f;
            float dr = r2 - (strokeWidth + accentStrokeWidth) / 2.0f;

            // place the neck in an Y-position proportional to the rate between r1 and r2, with m1+r1Y to the top leading to c1.Y, and m2+r2Y to the bot leading to c2.Y
            float m1 = (2 * r - (r1 + r2)) / (1 + r2 / r1);
            float m2 = m1 * r2 / r1;
            // position of the neck
            float nY = c1.Y + r1 + m1;
            float nX = c1.X;
            // Control points: a1*2 is on a line from nY,nX-neckX to c1.Y,c.X-r1 where it intersects c1.Y+r1; we divide it by 2 to make the curve smoother
            float a1 = (m1 * (r1 - neckX) / (r1 + m1)) / 2;
            float a2 = (m2 * (r2 - neckX) / (r2 + m2)) / 2;

            var path = new CGPath();
            path.MoveToPoint(CG.Point(swipe % new SKPoint(c1.X - r1, c1.Y)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c1.X - r1, c1.Y + 0.5f * r1)), CG.Point(swipe % new SKPoint(nX - (neckX + a1), c1.Y + r1)), CG.Point(swipe % new SKPoint(nX - neckX, nY)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(nX - (neckX + a2), c2.Y - r2)), CG.Point(swipe % new SKPoint(c2.X - r2, c2.Y - 0.5f * r2)), CG.Point(swipe % new SKPoint(c2.X - r2, c2.Y)));

            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c2.X - r2, c2.Y + 0.75f * r2)), CG.Point(swipe % new SKPoint(c2.X - 0.75f * r2, c2.Y + r2)), CG.Point(swipe % new SKPoint(c2.X, c2.Y + r2)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c2.X + 0.75f * r2, c2.Y + r2)), CG.Point(swipe % new SKPoint(c2.X + r2, c2.Y + 0.75f * r2)), CG.Point(swipe % new SKPoint(c2.X + r2, c2.Y)));

            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c2.X + r2, c2.Y - 0.5f * r2)), CG.Point(swipe % new SKPoint(nX + (neckX + a2), c2.Y - r2)), CG.Point(swipe % new SKPoint(nX + neckX, nY)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(nX + (neckX + a1), c1.Y + r1)), CG.Point(swipe % new SKPoint(c1.X + r1, c1.Y + 0.5f * r1)), CG.Point(swipe % new SKPoint(c1.X + r1, c1.Y)));

            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c1.X + r1, c1.Y - 0.75f * r1)), CG.Point(swipe % new SKPoint(c1.X + 0.75f * r1, c1.Y - r1)), CG.Point(swipe % new SKPoint(c1.X, c1.Y - r1)));
            path.AddCurveToPoint(CG.Point(swipe % new SKPoint(c1.X - 0.75f * r1, c1.Y - r1)), CG.Point(swipe % new SKPoint(c1.X - r1, c1.Y - 0.75f * r1)), CG.Point(swipe % new SKPoint(c1.X - r1, c1.Y)));
            path.CloseSubpath();

            var darkPath = new CGPath();
            darkPath.MoveToPoint(CG.Point(swipe % new SKPoint(c2.X, c2.Y + dr)));
            darkPath.AddCurveToPoint(CG.Point(swipe % new SKPoint(c2.X + 0.75f * dr, c2.Y + dr)), CG.Point(swipe % new SKPoint(c2.X + dr, c2.Y + 0.75f * dr)), CG.Point(swipe % new SKPoint(c2.X + dr, c2.Y)));

            var lightPath = new CGPath();
            lightPath.MoveToPoint(CG.Point(swipe % new SKPoint(c1.X, c1.Y - lr)));
            lightPath.AddCurveToPoint(CG.Point(swipe % new SKPoint(c1.X - 0.75f * lr, c1.Y - lr)), CG.Point(swipe % new SKPoint(c1.X - lr, c1.Y - 0.75f * lr)), CG.Point(swipe % new SKPoint(c1.X - lr, c1.Y)));

            using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 191), StrokeWidth = swipe % strokeWidth, IsAntialias = true })
            using (var accentLightPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(255, 255, 255, 191), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            using (var accentDarkPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 95), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            {

                canvas.AddPath(path);
                canvas.SetFillColor(CG.Color(fillPaint.Color));
                canvas.FillPath();

                canvas.AddPath(darkPath);
                canvas.SetStrokeColor(CG.Color(accentDarkPaint.Color));
                canvas.SetLineWidth(accentDarkPaint.StrokeWidth);
                canvas.SetLineJoin(CG.LineJoin(accentDarkPaint.StrokeJoin));
                canvas.SetLineCap(CG.LineCap(accentDarkPaint.StrokeCap));
                canvas.StrokePath();

                canvas.AddPath(lightPath);
                canvas.SetStrokeColor(CG.Color(accentLightPaint.Color));
                canvas.SetLineWidth(accentLightPaint.StrokeWidth);
                canvas.SetLineJoin(CG.LineJoin(accentLightPaint.StrokeJoin));
                canvas.SetLineCap(CG.LineCap(accentLightPaint.StrokeCap));
                canvas.StrokePath();

                canvas.AddPath(path);
                canvas.SetStrokeColor(CG.Color(strokePaint.Color));
                canvas.SetLineWidth(strokePaint.StrokeWidth);
                canvas.SetLineJoin(CG.LineJoin(strokePaint.StrokeJoin));
                canvas.SetLineCap(CG.LineCap(strokePaint.StrokeCap));
                canvas.StrokePath();
            }

            float rYtext = (r1 == r2) ? ((dir == ProtocolDevice.Direction.Top) ? r1 : r2) : (r1 > r2) ? r1 : r2;
            SKPoint cText = (r1 == r2) ? ((dir == ProtocolDevice.Direction.Top) ? c1 : c2) : (r1 > r2) ? c1 : c2;
            DrawDropletLabel(canvas, label, cText, rYtext, textStrokeWidth, biggie, swipe);
        }

        public void DrawPad(CGContext canvas, SKPoint center, float radius, SKPaint strokePaint, float strokePaintStrokeWidth, SKPaint fillPaint, SKPaint holePaint, SKPaint strokeAccentPaint, float strokeAccentPaintStrokeWidth, SKPaint holeAccentPaint, Swipe swipe)
        {
            float step = radius / 7.0f;
            float holeRadius = radius / 7.0f;
            float orthShift = (strokePaintStrokeWidth + strokeAccentPaintStrokeWidth) / 2.0f;
            float diagShift = orthShift / 1.41f;

            var path = new CGPath();
            path.MoveToPoint(CG.Point(swipe % new SKPoint(center.X - radius, center.Y - radius)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius, center.Y - radius + 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 1 * step, center.Y - radius + 2 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius - 1 * step, center.Y - radius + 4 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 1 * step, center.Y - radius + 6 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius - 1 * step, center.Y - radius + 8 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 1 * step, center.Y - radius + 10 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius - 1 * step, center.Y - radius + 12 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius, center.Y - radius + 13 * step)));

            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius, center.Y + radius)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 1 * step, center.Y + radius)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 2 * step, center.Y + radius - 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 4 * step, center.Y + radius + 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 6 * step, center.Y + radius - 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 8 * step, center.Y + radius + 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 10 * step, center.Y + radius - 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 12 * step, center.Y + radius + 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + 13 * step, center.Y + radius)));

            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius, center.Y + radius)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius, center.Y + radius - 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 1 * step, center.Y + radius - 2 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius + 1 * step, center.Y + radius - 4 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 1 * step, center.Y + radius - 6 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius + 1 * step, center.Y + radius - 8 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 1 * step, center.Y + radius - 10 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius + 1 * step, center.Y + radius - 12 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius, center.Y + radius - 13 * step)));

            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius, center.Y - radius)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 1 * step, center.Y - radius)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 2 * step, center.Y - radius + 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 4 * step, center.Y - radius - 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 6 * step, center.Y - radius + 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 8 * step, center.Y - radius - 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 10 * step, center.Y - radius + 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 12 * step, center.Y - radius - 1 * step)));
            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius - 13 * step, center.Y - radius)));

            path.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius, center.Y - radius)));
            path.CloseSubpath();

            var accPathLft = new CGPath();
            accPathLft.MoveToPoint(CG.Point(swipe % new SKPoint(center.X - radius + orthShift, center.Y - radius + orthShift)));
            accPathLft.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + orthShift, center.Y - radius + orthShift + 1 * step)));
            accPathLft.MoveToPoint(CG.Point(swipe % new SKPoint(center.X - radius + diagShift + 1 * step, center.Y - radius + diagShift + 2 * step)));
            accPathLft.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + diagShift - 1 * step, center.Y - radius + diagShift + 4 * step)));
            accPathLft.MoveToPoint(CG.Point(swipe % new SKPoint(center.X - radius + diagShift + 1 * step, center.Y - radius + diagShift + 6 * step)));
            accPathLft.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + diagShift - 1 * step, center.Y - radius + diagShift + 8 * step)));
            accPathLft.MoveToPoint(CG.Point(swipe % new SKPoint(center.X - radius + diagShift + 1 * step, center.Y - radius + diagShift + 10 * step)));
            accPathLft.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + diagShift - 1 * step, center.Y - radius + diagShift + 12 * step)));
            accPathLft.MoveToPoint(CG.Point(swipe % new SKPoint(center.X - radius + orthShift, center.Y - radius + 13 * step)));
            accPathLft.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X - radius + orthShift, center.Y - radius + 14 * step)));

            var accPathTop = new CGPath();
            accPathTop.MoveToPoint(CG.Point(swipe % new SKPoint(center.X + radius, center.Y - radius + orthShift)));
            accPathTop.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius + diagShift / 2 - 1 * step, center.Y - radius + orthShift)));
            accPathTop.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius + diagShift - 2 * step, center.Y - radius + diagShift + 1 * step)));
            accPathTop.MoveToPoint(CG.Point(swipe % new SKPoint(center.X + radius + diagShift - 4 * step, center.Y - radius + diagShift - 1 * step)));
            accPathTop.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius + diagShift - 6 * step, center.Y - radius + diagShift + 1 * step)));
            accPathTop.MoveToPoint(CG.Point(swipe % new SKPoint(center.X + radius + diagShift - 8 * step, center.Y - radius + diagShift - 1 * step)));
            accPathTop.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius + diagShift - 10 * step, center.Y - radius + diagShift + 1 * step)));
            accPathTop.MoveToPoint(CG.Point(swipe % new SKPoint(center.X + radius + diagShift - 12 * step, center.Y - radius + diagShift - 1 * step)));
            accPathTop.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius + diagShift / 2 - 13 * step, center.Y - radius + orthShift)));
            accPathTop.AddLineToPoint(CG.Point(swipe % new SKPoint(center.X + radius + orthShift - 14 * step, center.Y - radius + orthShift)));

            canvas.AddPath(path);
            canvas.SetFillColor(CG.Color(fillPaint.Color));
            canvas.FillPath();

            canvas.AddPath(accPathLft);
            canvas.SetStrokeColor(CG.Color(strokeAccentPaint.Color));
            canvas.SetLineWidth(strokeAccentPaint.StrokeWidth);
            canvas.SetLineJoin(CG.LineJoin(strokeAccentPaint.StrokeJoin));
            canvas.SetLineCap(CG.LineCap(strokeAccentPaint.StrokeCap));
            canvas.StrokePath();

            canvas.AddPath(accPathTop);
            canvas.SetStrokeColor(CG.Color(strokeAccentPaint.Color));
            canvas.SetLineWidth(strokeAccentPaint.StrokeWidth);
            canvas.SetLineJoin(CG.LineJoin(strokeAccentPaint.StrokeJoin));
            canvas.SetLineCap(CG.LineCap(strokeAccentPaint.StrokeCap));
            canvas.StrokePath();

            canvas.AddPath(path);
            canvas.SetStrokeColor(CG.Color(strokePaint.Color));
            canvas.SetLineWidth(strokePaint.StrokeWidth);
            canvas.SetLineJoin(CG.LineJoin(strokePaint.StrokeJoin));
            canvas.SetLineCap(CG.LineCap(strokePaint.StrokeCap));
            canvas.StrokePath();

            canvas.AddEllipseInRect(CG.RectFromCircle(swipe % new SKPoint(center.X + diagShift / 2, center.Y + diagShift / 2), swipe % (holeRadius + diagShift / 2)));
            canvas.SetFillColor(CG.Color(holeAccentPaint.Color));
            canvas.FillPath();
            canvas.AddEllipseInRect(CG.RectFromCircle(swipe % center, swipe % holeRadius));
            canvas.SetFillColor(CG.Color(holePaint.Color));
            canvas.FillPath();
        }

    }
}
