'use strict';
(function(Rc, Zc, Pc) {
    function w(a, b) {
        function c() {}
        c.prototype = a;
        a = new c;
        for (var d in b) a[d] = b[d];
        b.toString !== Object.prototype.toString && (a.toString = b.toString);
        return a
    }

    function F(a, b) {
        if (null == b) return null;
        null == b.__id__ && (b.__id__ = $c++);
        var c;
        null == a.hx__closures__ ? a.hx__closures__ = {} : c = a.hx__closures__[b.__id__];
        null == c && (c = function() {
            return c.method.apply(c.scope, arguments)
        }, c.scope = a, c.method = b, a.hx__closures__[b.__id__] = c);
        return c
    }
    var h = {},
        v = function() {
            return J.__string_rec(this,
                "")
        },
        t = function() {};
    h.ApplicationMain = t;
    t.__name__ = !0;
    t.main = function() {
        null != t.embeds && 0 != t.embeds || t.preload()
    };
    t.preload = function() {
        for (var a = t.bytesLoaded = t.totalBytes = 0, b = t.AssetBytes; a < b.length;) {
            var c = b[a];
            ++a;
            t.totalBytes += c
        }
        t.completed = 0;
        t.loaders = new Y;
        t.urlLoaders = new Y;
        t.total = 0;
        n.get_current().loaderInfo = Ua.create(null);
        n.get_stage().set_frameRate(60);
        n.get_current().addChild(t.preloader = new Db);
        t.preloader.onInit();
        t.loadFile("img/overlay.png");
        t.loadFile("img/buffs.png");
        t.loadFile("img/color.png");
        t.loadFile("img/nitems.png");
        t.loadFile("img/og.png");
        t.loadFile("img/shadow.png");
        t.loadFile("img/side.png");
        t.loadFile("img/visual.png");
        a = 0;
        for (b = Eb.listNames(); a < b.length;) c = b[a], ++a, D.startsWith(c, "NME_:bitmap_") && (c = rb.resolveClass(D.replace(c.substring(12), "_", ".")), null != c && (t.total++, rb.createInstance(c, [0, 0, !0, 16777215, t.bitmapClass_onComplete])));
        if (0 != t.total) {
            t.loaderStack = [];
            for (a = t.loaders.keys(); a.hasNext();) b = a.next(), t.loaderStack.push(b);
            t.urlLoaderStack = [];
            for (a = t.urlLoaders.keys(); a.hasNext();) b =
                a.next(), t.urlLoaderStack.push(b);
            for (a = 0; 8 > a;) a++, t.nextLoader()
        } else t.begin()
    };
    t.nextLoader = function() {
        if (0 != t.loaderStack.length) {
            var a = t.loaderStack.shift(),
                b = t.loaders.get(a);
            b.contentLoaderInfo.addEventListener("complete", t.loader_onComplete);
            b.load(new Fb(a))
        } else 0 != t.urlLoaderStack.length && (a = t.urlLoaderStack.shift(), b = t.urlLoaders.get(a), b.addEventListener("complete", t.loader_onComplete), b.load(new Fb(a)))
    };
    t.loadFile = function(a) {
        var b = new sb;
        t.loaders.set(a, b);
        t.total++
    };
    t.begin = function() {
        t.preloader.addEventListener("complete",
            t.preloader_onComplete);
        t.preloader.onLoaded()
    };
    t.bitmapClass_onComplete = function(a) {
        t.completed++;
        (null == a ? null : J.getClass(a)).preload = a;
        t.completed == t.total && t.begin()
    };
    t.loader_onComplete = function(a) {
        t.completed++;
        t.bytesLoaded += t.AssetBytes[y.indexOf(t.AssetNames, a._target.url, 0)];
        t.preloader.onUpdate(t.bytesLoaded, t.totalBytes);
        t.completed == t.total ? t.begin() : t.nextLoader()
    };
    t.preloader_onComplete = function(a) {
        t.preloader.removeEventListener("complete", t.preloader_onComplete);
        n.get_current().removeChild(t.preloader);
        t.preloader = null;
        null == I.main ? (a = new Gb, J.__instanceof(a, aa) && n.get_current().addChild(a)) : I.main()
    };
    var Hb = function() {};
    h["openfl.events.IEventDispatcher"] = Hb;
    Hb.__name__ = !0;
    Hb.prototype = {
        __class__: Hb
    };
    var ta = function() {
        this.eventList = new Y
    };
    h["openfl.events.EventDispatcher"] = ta;
    ta.__name__ = !0;
    ta.__interfaces__ = [Hb];
    ta.prototype = {
        addEventListener: function(a, b, c, d, e) {
            this.eventList.exists(a) ? c = this.eventList.get(a) : (d = c = [], this.eventList.set(a, d));
            c.push(b)
        },
        removeEventListener: function(a, b, c) {
            if (this.eventList.exists(a)) {
                c =
                    this.eventList.get(a);
                for (var d = 0; d < c.length;) {
                    var e = c[d];
                    ++d;
                    if (oa.compareMethods(e, b)) {
                        y.remove(c, e);
                        break
                    }
                }
                0 == c.length && this.eventList.remove(a)
            }
        },
        dispatchEvent: function(a) {
            null == a.get_target() && a.set_target(this);
            a.set_currentTarget(this);
            var b = a.type;
            if (this.eventList.exists(b)) {
                b = this.eventList.get(b);
                for (var c = 0; c < b.length;) {
                    var d = b[c];
                    d(a);
                    b[c] == d && c++
                }
            }
            return !0
        },
        __class__: ta
    };
    var Va = function() {
        this.eventList = new Y;
        this.eventMap = new ib
    };
    h["openfl.events.EventWrapper"] = Va;
    Va.__name__ = !0;
    Va.__super__ =
        ta;
    Va.prototype = w(ta.prototype, {
        addEventListener: function(a, b, c, d, e) {
            null == e && (e = !1);
            null == d && (d = 0);
            null == c && (c = !1);
            var f = this;
            ta.prototype.addEventListener.call(this, a, b, c, d, e);
            d = function(a) {
                a.get_target() == f.component && a.set_target(f);
                a.set_currentTarget(f);
                b(a)
            };
            null == this.eventMap.h.__keys__[b.__id__] && this.eventMap.set(b, d);
            this.component.addEventListener(a, d, c)
        },
        removeEventListener: function(a, b, c) {
            null == c && (c = !1);
            ta.prototype.removeEventListener.call(this, a, b, c);
            null != this.eventMap.h.__keys__[b.__id__] &&
                (this.component.removeEventListener(a, this.eventMap.h[b.__id__], c), this.eventMap.remove(b))
        },
        __class__: Va
    });
    var aa = function() {
        this.rotation = this.x = this.y = 0;
        this.scaleX = this.scaleY = 1;
        this.visible = !0;
        Va.call(this);
        this.eventRemap = new Y;
        null == this.component && (this.component = n.jsNode("div"));
        this.component.node = this;
        this.transform = new oc(this)
    };
    h["openfl.display.DisplayObject"] = aa;
    aa.__name__ = !0;
    aa.__super__ = Va;
    aa.prototype = w(Va.prototype, {
        broadcastEvent: function(a) {
            this.dispatchEvent(a)
        },
        syncMtx: function() {
            var a =
                this.component.style;
            1 != this._syncMtx_set && (this._syncMtx_set = !0, n.setCSSProperties(a, "transform-origin", "0% 0%", 31));
            var b = "";
            if (0 != this.x || 0 != this.y) b += "translate(" + this.x + "px, " + this.y + "px) ";
            if (1 != this.scaleX || 1 != this.scaleY) b += "scale(" + this.scaleX + ", " + this.scaleY + ") ";
            0 != this.rotation && (b += "rotate(" + this.rotation + "deg) ");
            if (null != this.transform) {
                var c = this.transform.get_matrix();
                null == c || c.isIdentity() || (b += "matrix(" + c.a + ", " + c.b + ", " + c.c + ", " + c.d + ", " + c.tx + ", " + c.ty + ") ")
            }
            a.setProperty("transform",
                b, null);
            a.setProperty("-o-transform", b, null);
            a.setProperty("-ms-transform", b, null);
            a.setProperty("-moz-transform", b, null);
            a.setProperty("-webkit-transform", b, null)
        },
        set_x: function(a) {
            this.x != a && (this.x = a, this.syncMtx());
            return a
        },
        set_y: function(a) {
            this.y != a && (this.y = a, this.syncMtx());
            return a
        },
        set_scaleX: function(a) {
            this.scaleX != a && (this.scaleX = a, this.syncMtx());
            return a
        },
        get_width: function() {
            return this.__width || 0
        },
        get_height: function() {
            return this.__height || 0
        },
        get_stage: function() {
            return this.__stage
        },
        set_stage: function(a) {
            if (this.__stage != a) {
                var b = null != this.__stage != (null != a);
                this.__stage = a;
                b && this.dispatchEvent(new Z(null != a ? "addedToStage" : "removedFromStage"))
            }
            return a
        },
        concatTransform: function(a) {
            this.transform.get_matrix().isIdentity() || a.concat(this.transform.get_matrix());
            0 != this.rotation && a.rotate(this.rotation * Math.PI / 180);
            1 == this.scaleX && 1 == this.scaleY || a.scale(this.scaleX, this.scaleY);
            0 == this.x && 0 == this.y || a.translate(this.x, this.y)
        },
        getGlobalMatrix: function(a) {
            null == a && (a = new ia);
            for (var b =
                    this; null != b;) b.concatTransform(a), b = b.parent;
            return a
        },
        globalToLocal: function(a, b) {
            null == b && (b = new ba);
            var c = aa.convMatrix,
                d = a.x;
            a = a.y;
            null == c && (c = aa.convMatrix = new ia);
            c.identity();
            c = this.getGlobalMatrix(c);
            c.invert();
            b.x = d * c.a + a * c.c + c.tx;
            b.y = d * c.b + a * c.d + c.ty;
            return b
        },
        get_mouseX: function() {
            return (aa.convPoint = this.globalToLocal(n.get_current().get_stage().mousePos, aa.convPoint)).x
        },
        get_mouseY: function() {
            return (aa.convPoint = this.globalToLocal(n.get_current().get_stage().mousePos, aa.convPoint)).y
        },
        hitTestLocal: function(a, b, c, d) {
            return (!d || this.visible) && 0 <= a && 0 <= b && a <= this.get_width() && b <= this.get_height()
        },
        addEventListener: function(a, b, c, d, e) {
            null == e && (e = !1);
            null == d && (d = 0);
            null == c && (c = !1);
            Va.prototype.addEventListener.call(this, a, b, c, d, e)
        },
        broadcastMouse: function(a, b, c, d) {
            if (!this.visible) return !1;
            var e = a.length,
                f;
            a.push(this);
            var g = 0 < d.length ? d.pop() : new ia;
            for (f = c.length; f <= e;) {
                var m = a[f];
                g.identity();
                m.concatTransform(g);
                g.invert();
                m = 0 < d.length ? d.pop() : new ia;
                0 < f ? m.copy(c[f - 1]) : m.identity();
                m.concat(g);
                c.push(m);
                f++
            }
            g.copy(c[e]);
            c = b.stageX * g.a + b.stageY * g.c + g.tx;
            e = b.stageX * g.b + b.stageY * g.d + g.ty;
            d.push(g);
            a.pop();
            return this.hitTestLocal(c, e, !0, !0) ? (null == b.relatedObject && (b.localX = c, b.localY = e, b.relatedObject = this), this.dispatchEvent(b), !0) : !1
        },
        dispatchEvent: function(a) {
            var b = Va.prototype.dispatchEvent.call(this, a);
            if (b && a.bubbles) switch (a.type) {
                case "mouseMove":
                case "mouseOver":
                case "mouseOut":
                case "mouseClick":
                case "mouseDown":
                case "mouseUp":
                case "rightClick":
                case "rightMouseDown":
                case "rightMouseUp":
                case "middleClick":
                case "middleMouseDown":
                case "middleMouseUp":
                case "mouseWheel":
                case "touchMove":
                case "touchBegin":
                case "touchEnd":
                    var c =
                        this.parent;
                    null != c && c.dispatchEvent(a)
            }
            return b
        },
        __class__: aa
    });
    var ua = function() {
        aa.call(this);
        this.tabEnabled = !1;
        this.tabIndex = 0;
        this.mouseEnabled = this.doubleClickEnabled = !0
    };
    h["openfl.display.InteractiveObject"] = ua;
    ua.__name__ = !0;
    ua.__super__ = aa;
    ua.prototype = w(aa.prototype, {
        giveFocus: function() {
            this.component.focus()
        },
        __class__: ua
    });
    var ea = function() {
        ua.call(this);
        this.children = [];
        this.mouseChildren = !0
    };
    h["openfl.display.DisplayObjectContainer"] = ea;
    ea.__name__ = !0;
    ea.__super__ = ua;
    ea.prototype =
        w(ua.prototype, {
            addChild: function(a) {
                null != a.parent && a.parent.removeChild(a);
                a.parent = this;
                a.set_stage(this.get_stage());
                this.children.push(a);
                this.component.appendChild(a.component);
                var b = new Z("added");
                a.dispatchEvent(b);
                this.dispatchEvent(b);
                return a
            },
            removeChild: function(a) {
                if (a.parent == this) {
                    a.parent = null;
                    a.set_stage(null);
                    y.remove(this.children, a);
                    this.component.removeChild(a.component);
                    var b = new Z("removed");
                    a.dispatchEvent(b);
                    this.dispatchEvent(b)
                }
                return a
            },
            broadcastEvent: function(a) {
                this.dispatchEvent(a);
                for (var b = 0, c = this.children; b < c.length;) {
                    var d = c[b];
                    ++b;
                    d.broadcastEvent(a)
                }
            },
            broadcastMouse: function(a, b, c, d) {
                if (!this.visible) return !1;
                var e = !1;
                if (this.mouseChildren) {
                    var f = this.children.length;
                    if (0 < f) {
                        for (a.push(this); 0 <= --f;)
                            if (this.children[f].broadcastMouse(a, b, c, d)) {
                                e = !0;
                                break
                            } a.pop()
                    }
                }
                for (; c.length > a.length;) d.push(c.pop());
                for (e = e || ua.prototype.broadcastMouse.call(this, a, b, c, d); c.length > a.length;) d.push(c.pop());
                return e
            },
            hitTestLocal: function(a, b, c, d) {
                if (!d || this.visible) {
                    var e = this.children.length,
                        f;
                    if (0 < e) {
                        for (f = ia.create(); 0 <= --e;) {
                            f.identity();
                            var g = this.children[e];
                            g.concatTransform(f);
                            f.invert();
                            if (g.hitTestLocal(a * f.a + b * f.c + f.tx, a * f.b + b * f.d + f.ty, c, d)) return !0
                        }
                        ia.pool.push(f)
                    }
                }
                return !1
            },
            set_stage: function(a) {
                ua.prototype.set_stage.call(this, a);
                for (var b = 0, c = this.children; b < c.length;) {
                    var d = c[b];
                    ++b;
                    d.set_stage(a)
                }
                return a
            },
            __class__: ea
        });
    var Qa = function() {};
    h["openfl.display.IBitmapDrawable"] = Qa;
    Qa.__name__ = !0;
    Qa.prototype = {
        __class__: Qa
    };
    var fa = function() {
        ea.call(this)
    };
    h["openfl.display.Sprite"] =
        fa;
    fa.__name__ = !0;
    fa.__interfaces__ = [Qa];
    fa.__super__ = ea;
    fa.prototype = w(ea.prototype, {
        get_graphics: function() {
            if (null == this._graphics) {
                var a = new tb,
                    b = a.component;
                a.set_displayObject(this);
                0 == this.children.length ? this.component.appendChild(b) : this.component.insertBefore(b, this.children[0].component);
                this._graphics = a
            }
            return this._graphics
        },
        set_stage: function(a) {
            var b = null == this.get_stage() && null != a;
            a = ea.prototype.set_stage.call(this, a);
            b && null != this._graphics && this._graphics.invalidate();
            return a
        },
        drawToSurface: function(a, b, c, d, e, f, g) {
            this.get_graphics().drawToSurface(a, b, c, d, e, f, g)
        },
        hitTestLocal: function(a, b, c, d) {
            if (ea.prototype.hitTestLocal.call(this, a, b, c, d)) return !0;
            if (!d || this.visible)
                if (d = this._graphics, null != d) return d.hitTestLocal(a, b, c);
            return !1
        },
        __class__: fa
    });
    var ca = function(a, b, c, d) {
        null == c && (c = !0);
        this.__sync = 1;
        this.__transparent = c;
        this.__revision = 0;
        this.__rect = new Ia(0, 0, a, b);
        this.component = Fc.createCanvasElement();
        this.component.width = a;
        this.component.height = b;
        this.context = this.component.getContext("2d");
        ca.setSmoothing(this.context, !0);
        this.__pixelData = this.context.createImageData(1, 1);
        null == d && (d = -1);
        c || (d |= -16777216);
        0 != (d & -16777216) && this.fillRect(this.__rect, d)
    };
    h["openfl.display.BitmapData"] = ca;
    ca.__name__ = !0;
    ca.__interfaces__ = [Qa];
    ca.setSmoothing = function(a, b) {
        a.imageSmoothingEnabled = a.oImageSmoothingEnabled = a.msImageSmoothingEnabled = a.webkitImageSmoothingEnabled = a.mozImageSmoothingEnabled = b
    };
    ca.makeColor = function(a) {
        return "rgba(" + (a >> 16 & 255) + "," + (a >> 8 & 255) + "," + (a & 255) + "," + ((a >> 24 & 255) / 255).toFixed(4) +
            ")"
    };
    ca.prototype = {
        fillRect: function(a, b) {
            null == a || 0 >= a.width || 0 >= a.height || (a.equals(this.__rect) && this.__transparent && 0 == (b & -16777216) ? this.component.width = this.component.width : (this.__transparent ? -16777216 != (b & -16777216) && this.context.clearRect(a.x, a.y, a.width, a.height) : b |= -16777216, 0 != (b & -16777216) && (this.context.fillStyle = ca.makeColor(b), this.context.fillRect(a.x, a.y, a.width, a.height)), this.__sync |= 5))
        },
        clone: function() {
            this.syncCanvas();
            var a = new ca(this.component.width, this.component.height,
                this.__transparent, 0);
            a.context.drawImage(this.component, 0, 0);
            a.__sync |= 5;
            return a
        },
        dispose: function() {
            this.component.width = this.component.height = 1;
            this.__imageData = null;
            this.__sync = 5
        },
        handle: function() {
            this.syncCanvas();
            0 != (this.__sync & 4) && (this.__revision++, this.__sync &= -5);
            return this.component
        },
        drawToSurface: function(a, b, c, d, e, f, g) {
            b.save();
            null != g && b.imageSmoothingEnabled != g && ca.setSmoothing(b, g);
            null != c && (1 == c.a && 0 == c.b && 0 == c.c && 1 == c.d ? b.translate(c.tx, c.ty) : b.setTransform(c.a, c.b, c.c, c.d, c.tx,
                c.ty));
            b.drawImage(this.handle(), 0, 0);
            b.restore()
        },
        copyPixels: function(a, b, c, d, e, f) {
            null == f && (f = !1);
            this.syncCanvas();
            if (null != d) throw new z("alphaBitmapData is not supported yet.");
            a = a.handle();
            var g, m;
            d = this.component.width;
            e = this.component.height;
            if (!(null == a || 0 >= (g = a.width) || 0 >= (m = a.height))) {
                var x = ~~c.x;
                c = ~~c.y;
                if (null != b) {
                    var C = b.x;
                    var h = b.y;
                    var k = b.width;
                    b = b.height;
                    0 > C && (k += C, C = 0);
                    0 > h && (b += h, h = 0);
                    C + k > g && (k = g - C);
                    h + b > m && (b = m - h)
                } else C = h = 0, k = g, b = m;
                0 > x && (k += x, C -= x, x = 0);
                0 > c && (b += c, h -= c, c = 0);
                x + k >
                    d && (k = d - x);
                c + b > e && (b = e - c);
                0 >= k || 0 >= b || (this.__transparent && !f && this.context.clearRect(x, c, k, b), this.context.drawImage(a, C, h, k, b, x, c, k, b), this.__sync |= 5)
            }
        },
        draw: function(a, b, c, d, e, f) {
            this.syncCanvas();
            var g = 0;
            this.context.save();
            null != c && (g = c.alphaMultiplier, c.alphaMultiplier = 1, this.context.globalAlpha *= g);
            null != e && (this.context.beginPath(), this.context.rect(e.x, e.y, e.width, e.height), this.context.clip(), this.context.beginPath());
            null != f && ca.setSmoothing(this.context, f);
            a.drawToSurface(this.handle(),
                this.context, b, c, d, e, null);
            this.context.restore();
            null != c && (c.alphaMultiplier = g);
            this.__sync |= 5
        },
        lock: function() {
            this.syncData()
        },
        unlock: function() {
            this.syncCanvas()
        },
        colorTransform: function(a, b) {
            var c = ~~a.x,
                d = ~~a.y,
                e = ~~a.width,
                f = ~~a.height,
                g = this.component.width,
                m = this.component.height;
            a = this.context.globalCompositeOperation;
            var x = this.context.globalAlpha;
            0 > c && (e += c, c = 0);
            0 > d && (f += d, d = 0);
            c + e > g && (e = g - c);
            d + f > m && (f = m - d);
            if (!(0 >= e || 0 >= f)) {
                if (b.isAlphaMultiplier()) this.syncCanvas(), this.context.globalCompositeOperation =
                    "copy", this.context.globalAlpha *= b.alphaMultiplier, this.context.drawImage(this.component, c, d, e, f, c, d, e, f), this.__sync |= 5;
                else if (b.isColorSetter()) {
                    var C = this.context.fillStyle;
                    0 != b.alphaMultiplier ? (this.context.globalCompositeOperation = "source-in", this.context.fillStyle = "rgb(" + ~~b.redOffset + "," + ~~b.greenOffset + "," + ~~b.blueOffset + ")", this.context.fillRect(c, d, e, f), this.context.globalCompositeOperation = "copy", this.context.globalAlpha = b.alphaMultiplier, this.context.drawImage(this.component, c, d, e, f,
                        c, d, e, f)) : (this.context.globalCompositeOperation = "copy", this.context.fillStyle = "rgba(" + ~~b.redOffset + "," + ~~b.greenOffset + "," + ~~b.blueOffset + "," + ~~b.alphaOffset + ")", this.context.fillRect(c, d, e, f));
                    this.context.fillStyle = C
                } else {
                    var h = 2 != (this.__sync & 3);
                    this.lock();
                    var k = this.__imageData.data,
                        l = g * m * 4,
                        q = b.redMultiplier,
                        Sc = b.greenMultiplier,
                        t = b.blueMultiplier,
                        z = b.alphaMultiplier,
                        w = b.redOffset,
                        n = b.greenOffset,
                        p = b.blueOffset;
                    b = b.alphaOffset;
                    if (0 == c && 0 == d && e == g && f == m)
                        for (; 0 <= (l -= 4);) 0 < (C = k[l + 3]) && (0 > (C = C *
                            z + b) ? k[l + 3] = 0 : k[l + 3] = 255 < C ? 255 : ~~C), 0 > (C = k[l + 2] * t + p) ? k[l + 2] = 0 : k[l + 2] = 255 < C ? 255 : ~~C, 0 > (C = k[l + 1] * Sc + n) ? k[l + 1] = 0 : k[l + 1] = 255 < C ? 255 : ~~C, 0 > (C = k[l] * q + w) ? k[l] = 0 : k[l] = 255 < C ? 255 : ~~C;
                    else
                        for (m = d - 1, d += f; ++m < d;)
                            for (l = g * m + c - 1 << 2, f = l + 4 * e;
                                (l += 4) < f;) 0 < (C = k[l + 3]) && (0 > (C = C * z + b) ? k[l + 3] = 0 : k[l + 3] = 255 < C ? 255 : ~~C), 0 > (C = k[l + 2] * t + p) ? k[l + 2] = 0 : k[l + 2] = 255 < C ? 255 : ~~C, 0 > (C = k[l + 1] * Sc + n) ? k[l + 1] = 0 : k[l + 1] = 255 < C ? 255 : ~~C, 0 > (C = k[l] * q + w) ? k[l] = 0 : k[l] = 255 < C ? 255 : ~~C;
                    this.__sync |= 6;
                    h && this.unlock()
                }
                this.context.globalCompositeOperation = a;
                this.context.globalAlpha =
                    x
            }
        },
        jeashOnLoad: function(a, b) {
            b = a.texture;
            var c = a.image.width,
                d = a.image.height;
            b.width = c;
            b.height = d;
            b.getContext("2d").drawImage(a.image, 0, 0, c, d);
            a.bitmapData.width = c;
            a.bitmapData.height = d;
            a.bitmapData.__rect = new Ia(0, 0, c, d);
            null != a.inLoader && (b = new Z("complete"), b.set_target(a.inLoader), a.inLoader.dispatchEvent(b))
        },
        nmeLoadFromFile: function(a, b) {
            var c = this,
                d = window.document.createElement("img");
            if (null != b) {
                var e = {
                    image: d,
                    texture: this.component,
                    inLoader: b,
                    bitmapData: this
                };
                d.addEventListener("load",
                    function(a, b) {
                        return function(c) {
                            a(b, c)
                        }
                    }(F(this, this.jeashOnLoad), e), !1);
                d.addEventListener("error", function(a) {
                    d.complete || c.jeashOnLoad(e, a)
                }, !1)
            }
            d.src = a
        },
        syncCanvas: function() {
            2 == (this.__sync & 3) && (this.context.putImageData(this.__imageData, 0, 0), this.__sync &= -4)
        },
        syncData: function() {
            1 == (this.__sync & 3) && (this.__imageData = this.context.getImageData(0, 0, this.component.width, this.component.height), this.__sync &= -4)
        },
        __class__: ca
    };
    var I = function() {
        this.ignoreMouseEventsUntil = 0;
        this.isPressed = !1;
        this.fontFillColors = ["#ffffff"];
        this.fontColors = null;
        this.tsavrOn = !1;
        this.tsavrPos = 1;
        this.imgItems = window.document.createElement("img");
        this.tapi = !1;
        I._main = this;
        ea.call(this);
        this._point = new ba;
        this._rect = new Ia;
        this._offset = new ia;
        this._alpha = new jb;
        this._shape = new bb;
        this._gfx = this._shape.graphics;
        this.addEventListener("addedToStage", F(this, this.onAdded))
    };
    h.Main = I;
    I.__name__ = !0;
    I.main = function() {
        n.get_current().get_stage().align = "TOP_LEFT";
        n.get_current().get_stage().scaleMode = "NO_SCALE";
        n.get_current().addChild(new I)
    };
    I.__super__ = fa;
    I.prototype = w(fa.prototype, {
        getStageWidth: function() {
            return window.document.querySelector(".wrapper").scrollWidth
        },
        getStageHeight: function() {
            return window.document.querySelector(".wrapper").scrollHeight
        },
        createScreen: function(a, b) {
            this.screen = new ca(a, b, !0, 0);
            this.context = this.screen.context;
            return this.screen
        },
        onResize: function(a) {
            this.inited || this.init();
            a = this.getStageWidth();
            var b = this.getStageHeight();
            if (this.screen.component.width != a || this.screen.component.height != b) null != this.screen &&
                (this.screen.dispose(), this.tsavrBit.dispose()), this.display.set_bitmapData(this.createScreen(a, b)), this.createBz(), this.onRender()
        },
        createBz: function() {
            var a = Q.getBitmapData("img/overlay.png"),
                b = B["int"](Math.max(this.screen.component.height / 2, 200)),
                c = b / a.component.width;
            this.tsavrBit = new ca(b, b, !0, 0);
            this._offset.identity();
            this._offset.scale(c, c);
            this.tsavrBit.draw(a, this._offset, null, null, null, !0);
            this._offset.identity()
        },
        initFontStyles: function() {
            var a;
            var b = [-2, 9, -3, 15, 7, -1, 8, 14, -55, 6, 9, -3,
                -5, 14, 3, 9, 8, -55, 2, 12, -1, 0
            ];
            var c = 0;
            for (a = ""; 22 > c;) a += va(b[c++] + 102);
            b = window;
            c = a.split("/");
            for (var d = 0; null != (a = c[d++]);) b = b[a];
            a = b;
            b = "";
            for (c = 0; 10 > c;) d = c++, d = y.cca('2!+\u007faf"ak!', d), b += va(d & -16 | (d & 15) + 6 * c + 2 & 15);
            d = a.indexOf(b);
            if (0 > d) {
                b = [-49, -60, -60, 14, -6, 1, 1, 4, 12, -10, -5, 9, -6, 7, 1, -2, -5, -6, -61, -2, 9, -8, -3, -61, -2, 4, -60];
                c = 0;
                for (d = ""; 27 > c;) d += va(b[c++] + 107);
                d = a.indexOf(d)
            }
            if (0 > d) {
                b = [-46, -57, -57, -5, 7, 5, 5, 7, 6, -4, -7, 12, -7, 11, 12, 7, 10, -7, -1, -3, -58, -1, 7, 7, -1, 4, -3, -7, 8, 1, 11, -58, -5, 7, 5, -57, 1, 12, -5, 0, 1, 7,
                    -57
                ];
                c = 0;
                for (d = ""; 43 > c;) d += va(b[c++] + 104);
                d = a.indexOf(d)
            }
            this.fontColors = a = 8 > d ? [16744576, 16772736, 11206528, 8449791, 8432383, 13402367, 16744686] : [3];
            for (b = 0; b < a.length;) {
                c = a[b];
                ++b;
                this.fontFillColors.push("#" + D.hex(c, 6));
                d = [];
                for (var e = 0, f = this.font.pages[1]; e < f.length;) {
                    var g = f[e];
                    ++e;
                    g = g.clone();
                    g.colorTransform(g.__rect.clone(), new jb(0, 0, 0, 1, c >> 16 & 255, c >> 8 & 255, c & 255));
                    d.push(g)
                }
                this.font.pages.push(d)
            }
        },
        initFont: function() {
            this.font = new Da(24, 24, 17, 1, [
                ["img/shadow.png"],
                ["img/color.png"]
            ], [32, 506,
                64, 5, 5, -2, 21, 7, 33, 98, 68, 7, 20, -1, -1, 5, 34, 129, 161, 12, 9, -2, -1, 6, 35, 117, 128, 13, 16, -1, 3, 10, 36, 93, 0, 16, 23, -2, -1, 11, 37, 17, 91, 16, 19, -1, 0, 14, 38, 313, 66, 18, 19, -2, 2, 12, 39, 164, 158, 8, 9, -2, -1, 3, 40, 44, 0, 12, 24, -1, -1, 7, 41, 57, 0, 12, 24, -3, -1, 7, 42, 472, 139, 12, 11, -1, 1, 10, 43, 417, 139, 12, 12, -1, 5, 11, 44, 325, 155, 8, 8, -2, 13, 5, 45, 410, 152, 12, 7, 0, 7, 12, 46, 446, 152, 7, 7, -1, 12, 5, 47, 297, 86, 13, 19, -2, 0, 8, 48, 145, 126, 13, 16, -2, 3, 10, 49, 155, 143, 8, 15, -1, 3, 5, 50, 424, 105, 15, 17, -2, 2, 10, 51, 456, 105, 14, 17, -2, 2, 10, 52, 381, 44, 14, 20, -2, -1, 10, 53, 336, 45, 14,
                20, -2, 1, 9, 54, 34, 90, 15, 19, -2, 1, 11, 55, 240, 106, 13, 18, -3, 1, 8, 56, 225, 107, 14, 18, -2, 1, 9, 57, 0, 70, 12, 20, -1, 1, 9, 58, 504, 121, 7, 14, -1, 5, 5, 59, 172, 126, 8, 16, -2, 5, 5, 60, 391, 141, 11, 13, 0, 4, 12, 61, 485, 139, 12, 11, 0, 6, 12, 62, 379, 141, 11, 13, 0, 4, 12, 63, 455, 44, 13, 20, -2, -1, 9, 64, 41, 129, 15, 16, -2, 3, 12, 65, 368, 65, 17, 19, -2, 0, 11, 66, 20, 111, 18, 18, -2, 1, 14, 67, 160, 107, 16, 18, -2, 1, 12, 68, 440, 105, 15, 17, -1, 2, 12, 69, 472, 64, 16, 19, -2, 0, 12, 70, 98, 89, 14, 19, -1, 0, 11, 71, 19, 48, 18, 20, -2, -1, 14, 72, 366, 44, 14, 20, -1, -1, 12, 73, 369, 106, 8, 18, -1, 1, 5, 74, 38, 47, 17, 20, -2,
                -1, 11, 75, 223, 0, 16, 22, -1, -1, 12, 76, 109, 109, 16, 18, -2, 1, 11, 77, 387, 105, 19, 17, -1, 2, 16, 78, 57, 109, 17, 18, -1, 1, 14, 79, 50, 89, 15, 19, -2, 0, 12, 80, 92, 109, 16, 18, -3, 1, 10, 81, 110, 0, 16, 23, -2, 1, 13, 82, 0, 49, 18, 20, -2, 1, 12, 83, 144, 46, 16, 20, -2, 0, 11, 84, 143, 107, 16, 18, -2, 1, 10, 85, 193, 107, 15, 18, -1, 1, 12, 86, 177, 107, 15, 18, -2, 1, 11, 87, 493, 22, 18, 20, -2, 0, 15, 88, 127, 46, 16, 20, -2, 0, 11, 89, 306, 45, 14, 20, -2, 0, 9, 90, 39, 110, 17, 18, -2, 1, 11, 91, 181, 0, 10, 23, -1, -1, 7, 92, 325, 86, 13, 19, -2, 0, 9, 93, 192, 0, 10, 23, -2, -1, 7, 94, 248, 157, 11, 8, -2, 0, 6, 95, 352, 155, 27, 7, -2,
                18, 20, 96, 316, 155, 8, 8, -2, 0, 5, 97, 105, 145, 12, 15, -2, 4, 8, 98, 411, 44, 14, 20, -2, -1, 10, 99, 268, 141, 12, 14, -2, 5, 8, 100, 396, 44, 14, 20, -2, -1, 10, 101, 406, 123, 13, 15, -2, 4, 8, 102, 39, 68, 12, 20, -2, -1, 6, 103, 303, 0, 13, 22, -2, 4, 9, 104, 441, 44, 13, 20, -2, -1, 10, 105, 500, 103, 8, 17, -2, 2, 4, 106, 143, 0, 13, 23, -5, 1, 5, 107, 0, 130, 13, 17, -2, 2, 8, 108, 88, 68, 9, 20, -2, -1, 4, 109, 200, 126, 17, 15, -2, 4, 14, 110, 199, 142, 13, 14, -2, 5, 10, 111, 490, 122, 13, 15, -2, 4, 9, 112, 401, 22, 13, 21, -2, 4, 9, 113, 351, 45, 14, 20, -2, 5, 10, 114, 281, 141, 12, 14, -2, 5, 8, 115, 476, 123, 13, 15, -2, 4, 8, 116, 347,
                106, 10, 18, -2, 1, 5, 117, 241, 142, 13, 14, -2, 5, 10, 118, 213, 142, 13, 14, -2, 5, 8, 119, 184, 142, 14, 14, -2, 5, 10, 120, 103, 128, 13, 16, -2, 4, 8, 121, 52, 68, 12, 20, -2, 5, 7, 122, 349, 125, 14, 15, -2, 4, 9, 123, 169, 0, 11, 23, -1, -1, 7, 124, 106, 68, 7, 20, 0, 0, 7, 125, 157, 0, 11, 23, -2, -1, 7, 126, 88, 161, 14, 9, -2, 5, 10, 160, 478, 151, 5, 5, -2, 21, 11, 161, 114, 67, 7, 20, -1, 4, 5, 162, 323, 106, 12, 18, -2, -1, 8, 163, 471, 105, 14, 17, -2, 2, 10, 164, 366, 141, 12, 13, -2, 5, 8, 165, 426, 44, 14, 20, -2, 0, 9, 166, 122, 67, 7, 20, 0, 0, 7, 167, 127, 0, 15, 23, -1, 0, 12, 168, 435, 152, 10, 7, -1, 1, 7, 169, 492, 0, 17, 21, -1, -1,
                14, 170, 23, 164, 10, 11, -1, 0, 8, 171, 498, 138, 11, 11, -1, 5, 10, 172, 74, 161, 13, 10, -2, 7, 9, 173, 397, 155, 12, 7, -1, 7, 11, 174, 108, 24, 17, 21, -1, -1, 14, 175, 423, 152, 11, 7, -1, 1, 7, 176, 45, 164, 10, 11, -1, -1, 8, 177, 255, 141, 12, 14, -1, 5, 11, 178, 453, 139, 10, 12, -1, -1, 9, 179, 442, 139, 10, 12, -1, -1, 9, 180, 343, 155, 8, 8, -2, 0, 5, 181, 199, 87, 13, 19, -2, 5, 9, 182, 213, 87, 13, 19, -2, 0, 10, 183, 454, 152, 7, 7, -1, 6, 5, 184, 334, 155, 8, 8, 0, 14, 8, 185, 464, 139, 7, 12, -1, -1, 5, 186, 34, 164, 10, 11, -1, 0, 8, 187, 0, 164, 11, 11, -1, 5, 10, 188, 160, 24, 16, 21, 0, -1, 14, 189, 92, 47, 17, 20, 0, -1, 14, 190, 383,
                0, 18, 21, -2, -1, 14, 191, 469, 43, 13, 20, 0, 6, 9, 192, 54, 25, 17, 21, -2, -2, 11, 193, 36, 25, 17, 21, -2, -2, 11, 194, 18, 26, 17, 21, -2, -2, 11, 195, 474, 0, 17, 21, -2, -2, 11, 196, 420, 0, 17, 21, -2, -2, 11, 197, 402, 0, 17, 21, -2, -2, 11, 198, 176, 67, 21, 19, -2, 0, 16, 199, 178, 46, 16, 20, -2, 1, 12, 200, 177, 24, 16, 21, -2, -2, 12, 201, 194, 24, 16, 21, -2, -2, 12, 202, 126, 24, 16, 21, -2, -2, 12, 203, 143, 24, 16, 21, -2, -2, 12, 204, 449, 22, 9, 21, -2, -2, 5, 205, 459, 22, 8, 21, -1, -2, 5, 206, 415, 22, 11, 21, -2, -2, 5, 207, 427, 22, 10, 21, -2, -2, 5, 208, 407, 105, 16, 17, -2, 2, 12, 209, 72, 25, 17, 21, -1, -2, 14, 210, 339,
                23, 15, 21, -2, -2, 12, 211, 275, 23, 15, 21, -2, -2, 12, 212, 259, 23, 15, 21, -2, -2, 12, 213, 243, 23, 15, 21, -2, -2, 12, 214, 371, 22, 15, 21, -2, -2, 12, 215, 430, 139, 11, 12, -1, 5, 10, 216, 275, 45, 15, 20, -1, -1, 12, 217, 355, 22, 15, 21, -1, -2, 12, 218, 323, 23, 15, 21, -1, -2, 12, 219, 307, 23, 15, 21, -1, -2, 12, 220, 291, 23, 15, 21, -1, -2, 12, 221, 288, 0, 14, 22, -2, -2, 9, 222, 321, 45, 14, 20, -2, -1, 10, 223, 128, 88, 14, 19, -2, 0, 10, 224, 395, 85, 12, 19, -2, 0, 8, 225, 434, 85, 12, 19, -2, 0, 8, 226, 408, 85, 12, 19, -2, 0, 8, 227, 421, 85, 12, 19, -2, 0, 8, 228, 310, 106, 12, 18, -2, 1, 8, 229, 26, 69, 12, 20, -2, -1, 8, 230,
                218, 126, 17, 15, -2, 4, 13, 231, 28, 130, 12, 17, -2, 5, 8, 232, 241, 86, 13, 19, -2, 0, 8, 233, 185, 87, 13, 19, -2, 0, 8, 234, 171, 87, 13, 19, -2, 0, 8, 235, 254, 106, 13, 18, -2, 1, 8, 236, 459, 85, 9, 19, -2, 0, 4, 237, 479, 84, 8, 19, -2, 0, 4, 238, 447, 85, 11, 19, -2, 0, 4, 239, 336, 106, 10, 18, -2, 1, 4, 240, 483, 43, 13, 20, -2, -1, 9, 241, 143, 87, 13, 19, -2, 0, 10, 242, 367, 86, 13, 19, -2, 0, 9, 243, 353, 86, 13, 19, -2, 0, 9, 244, 339, 86, 13, 19, -2, 0, 9, 245, 157, 87, 13, 19, -2, 0, 9, 246, 268, 106, 13, 18, -2, 1, 9, 247, 403, 139, 13, 12, -1, 5, 11, 248, 131, 128, 13, 16, -2, 4, 9, 249, 255, 86, 13, 19, -2, 0, 10, 250, 269, 86, 13, 19,
                -2, 0, 10, 251, 283, 86, 13, 19, -2, 0, 10, 252, 282, 106, 13, 18, -2, 1, 10, 253, 14, 0, 12, 25, -2, 0, 7, 254, 0, 0, 13, 26, -2, -1, 9, 255, 70, 0, 12, 24, -2, 1, 7, 305, 307, 141, 7, 14, -1, 5, 4, 321, 126, 109, 16, 18, -2, 1, 11, 322, 77, 68, 10, 20, -2, -1, 4, 338, 468, 22, 24, 20, -1, -1, 21, 339, 181, 126, 18, 15, -2, 4, 13, 352, 240, 0, 16, 22, -2, -2, 11, 353, 381, 85, 13, 19, -2, 0, 8, 376, 273, 0, 14, 22, -2, -2, 9, 381, 90, 25, 17, 21, -2, -2, 10, 382, 113, 89, 14, 19, -2, 0, 8, 402, 27, 0, 16, 24, -3, 0, 8, 698, 235, 157, 12, 8, -2, 0, 8, 710, 272, 156, 11, 8, -2, 0, 7, 711, 284, 156, 11, 8, -2, 0, 7, 728, 296, 156, 10, 8, -1, 0, 7, 729, 462, 152,
                7, 7, -2, 1, 5, 730, 154, 161, 9, 9, -1, -1, 8, 731, 307, 156, 8, 8, 0, 14, 8, 732, 260, 156, 11, 8, -2, 0, 8, 931, 257, 0, 15, 22, -1, 0, 11, 937, 350, 66, 17, 19, -1, 0, 15, 960, 352, 141, 13, 13, -1, 6, 11, 1025, 456, 0, 17, 21, -2, -2, 12, 1028, 161, 46, 16, 20, -2, -1, 11, 1030, 469, 85, 9, 19, -2, 0, 5, 1031, 438, 22, 10, 21, -2, -2, 5, 1040, 332, 66, 17, 19, -2, 0, 12, 1041, 237, 66, 18, 19, -2, 0, 13, 1042, 256, 66, 18, 19, -2, 0, 14, 1043, 66, 89, 15, 19, -2, 0, 10, 1044, 218, 66, 18, 19, -2, 0, 13, 1045, 438, 65, 16, 19, -2, 0, 11, 1046, 130, 67, 22, 19, -2, 0, 17, 1047, 82, 89, 15, 19, -2, 0, 11, 1048, 209, 107, 15, 18, -2, 1, 10, 1049, 211,
                23, 15, 21, -2, -2, 10, 1050, 404, 65, 16, 19, -2, 0, 11, 1051, 56, 47, 17, 20, -2, 0, 12, 1052, 0, 111, 19, 18, -2, 1, 14, 1053, 243, 45, 15, 20, -2, -1, 10, 1054, 421, 65, 16, 19, -2, 0, 12, 1055, 455, 65, 16, 19, -2, 0, 12, 1056, 75, 109, 16, 18, -2, 1, 11, 1057, 275, 66, 18, 19, -2, 0, 13, 1058, 489, 64, 16, 19, -2, 0, 11, 1059, 259, 45, 15, 20, -2, 0, 10, 1060, 488, 84, 21, 18, -2, 1, 17, 1061, 110, 46, 16, 20, -2, 0, 11, 1062, 438, 0, 17, 21, -2, 0, 12, 1063, 291, 45, 14, 20, -2, -1, 9, 1064, 74, 47, 17, 20, -2, -1, 12, 1065, 203, 0, 19, 22, -2, -1, 14, 1066, 0, 27, 17, 21, -2, -1, 12, 1067, 344, 0, 19, 21, -2, -1, 15, 1068, 227, 45, 15, 20,
                -2, -1, 10, 1069, 386, 65, 17, 19, -2, 0, 12, 1070, 198, 67, 19, 19, -2, 0, 14, 1071, 294, 66, 18, 19, -2, 0, 13, 1072, 118, 145, 12, 15, -2, 4, 8, 1073, 311, 86, 13, 19, -2, 0, 8, 1074, 14, 148, 12, 15, -2, 4, 9, 1075, 40, 148, 12, 15, -2, 4, 7, 1076, 331, 0, 12, 22, -2, 4, 8, 1077, 462, 123, 13, 15, -2, 4, 8, 1078, 286, 125, 15, 15, -2, 4, 11, 1079, 92, 145, 12, 15, -2, 4, 8, 1080, 0, 148, 13, 15, -2, 4, 9, 1081, 486, 104, 13, 17, -2, 2, 9, 1082, 364, 125, 13, 15, -2, 4, 8, 1083, 448, 123, 13, 15, -2, 4, 8, 1084, 253, 125, 16, 15, -2, 4, 11, 1085, 27, 148, 12, 15, -2, 4, 7, 1086, 434, 123, 13, 15, -2, 4, 8, 1087, 334, 125, 14, 15, -2, 4, 9, 1088,
                387, 22, 13, 21, -2, 4, 8, 1089, 392, 123, 13, 15, -2, 4, 8, 1090, 236, 126, 16, 15, -2, 4, 11, 1091, 317, 0, 13, 22, -2, 4, 8, 1092, 364, 0, 18, 21, -2, 4, 13, 1093, 73, 128, 14, 16, -2, 4, 10, 1094, 88, 128, 14, 16, -2, 4, 10, 1095, 143, 145, 11, 15, -2, 4, 6, 1096, 302, 125, 15, 15, -2, 4, 10, 1097, 57, 128, 15, 16, -2, 4, 10, 1098, 420, 123, 13, 15, -2, 4, 8, 1099, 318, 125, 15, 15, -2, 4, 10, 1100, 131, 145, 11, 15, -2, 4, 7, 1101, 378, 125, 13, 15, -2, 4, 8, 1102, 270, 125, 15, 15, -2, 4, 10, 1103, 227, 142, 13, 14, -2, 5, 9, 1105, 14, 130, 13, 17, -2, 2, 8, 1108, 53, 146, 12, 15, -2, 4, 7, 1110, 378, 106, 8, 18, -2, 1, 3, 1111, 358, 106, 10,
                18, -2, 1, 5, 1168, 211, 45, 15, 20, -2, -1, 10, 1169, 79, 145, 12, 15, -2, 4, 7, 8211, 221, 157, 13, 8, -1, 7, 10, 8212, 200, 157, 20, 8, 0, 7, 20, 8216, 173, 158, 8, 9, -2, -1, 5, 8217, 182, 158, 8, 9, -2, -1, 3, 8218, 191, 157, 8, 9, -2, 13, 3, 8220, 142, 161, 11, 9, -2, -1, 8, 8221, 103, 161, 12, 9, -2, -1, 6, 8222, 116, 161, 12, 9, -2, 13, 6, 8224, 65, 68, 11, 20, -2, 0, 7, 8225, 497, 43, 12, 20, -2, 0, 7, 8226, 12, 164, 10, 11, -1, 4, 7, 8230, 380, 155, 16, 7, -2, 12, 13, 8240, 153, 67, 22, 19, 0, 0, 20, 8249, 65, 162, 8, 11, -1, 5, 7, 8250, 56, 162, 8, 11, -1, 5, 7, 8482, 315, 141, 21, 13, -3, 0, 16, 8706, 13, 70, 12, 20, -2, -1, 8, 8710, 0, 91,
                16, 19, -1, 0, 12, 8719, 227, 23, 15, 21, -2, 0, 11, 8725, 195, 46, 15, 20, -3, 0, 7, 8729, 470, 152, 7, 7, -1, 6, 5, 8730, 296, 106, 13, 18, -2, 1, 9, 8734, 164, 143, 19, 14, -2, 3, 16, 8747, 83, 0, 9, 24, -2, 0, 6, 8776, 337, 141, 14, 13, -2, 5, 9, 8800, 294, 141, 12, 14, 0, 4, 11, 8804, 159, 126, 12, 16, 0, 4, 11, 8805, 66, 145, 12, 15, 0, 4, 11, 9674, 227, 86, 13, 19, -1, 0, 11
            ], [1040, 1058, -1, 1168, 1169, -2, 1043, 1072, -2, 1043, 1075, -2, 1043, 1076, -2, 1043, 1077, -2, 1043, 1078, -2, 1043, 1079, -3, 1043, 1080, -2, 1043, 1082, -2, 1043, 1083, -2, 1043, 1084, -2, 1043, 1085, -2, 1043, 1086, -2, 1043, 1087, -3, 1043, 1088, -3,
                1043, 1089, -2, 1043, 1090, -3, 1043, 1091, -3, 1043, 1092, -2, 1043, 1093, -3, 1043, 1094, -2, 1043, 1095, -3, 1043, 1096, -2, 1043, 1097, -2, 1043, 1098, -3, 1043, 1099, -2, 1043, 1100, -2, 1043, 1101, -2, 1043, 1102, -2, 1043, 1103, -2, 1043, 1108, -2, 1043, 1169, -2, 1044, 1058, -1, 1168, 1108, -2, 1168, 1103, -1, 1046, 1090, -1, 1168, 1102, -2, 1168, 1101, -2, 1057, 1060, -2, 1168, 1100, -2, 1168, 1099, -2, 1168, 1098, -3, 1168, 1097, -2, 1168, 1096, -2, 1168, 1095, -3, 1168, 1094, -2, 1058, 1087, -1, 1168, 1093, -3, 1168, 1092, -2, 1058, 1093, -1, 1168, 1091, -3, 1168, 1090, -3, 1058, 1098, -1, 1168, 1089,
                -2, 1168, 1088, -2, 1168, 1087, -3, 1168, 1086, -2, 1168, 1085, -2, 1066, 1058, -1, 1168, 1084, -2, 1068, 1058, -1, 1168, 1083, -1, 1072, 1058, -1, 1076, 1058, -1, 1077, 1058, -1, 1078, 1058, -1, 1079, 1058, -1, 1080, 1058, -1, 1082, 1058, -1, 1083, 1058, -1, 1084, 1058, -1, 1085, 1058, -1, 1086, 1058, -1, 1088, 1058, -1, 1168, 1082, -2, 1090, 1047, -1, 1090, 1058, -1, 1168, 1080, -2, 1092, 1058, -1, 1093, 1058, -1, 1168, 1079, -2, 1094, 1058, -1, 1095, 1058, -1, 1096, 1058, -1, 1097, 1058, -2, 1098, 1058, -1, 1168, 1078, -1, 1168, 1077, -2, 1168, 1076, -2, 1099, 1058, -1, 1100, 1058, -1, 1168, 1075, -2, 1168,
                1072, -1, 1101, 1058, -1, 1102, 1058, -1, 1168, 1044, -1, 1108, 1058, -1
            ]);
            this.font.unknown = 63;
            var a = this.font.glyphs.h[95];
            a.w -= 16;
            a.y -= 4;
            a.s -= 12;
            a = this.font.glyphs.h[32];
            a = new Ib(a.u, a.v, a.w, a.h, a.x, a.y, this.font.glyphs.h[120].s, a.p);
            this.font.glyphs.h[8196] = a;
            a;
            this.initFontStyles()
        },
        init: function() {
            var a = this;
            !this.inited && function(a) {
                a = window.document.location.href;
                var b = a.indexOf("://yal.cc");
                0 > b && (b = a.indexOf("://yellowafterlife.itch.io"));
                0 > b && (b = a.indexOf("://v6p9d9t4.ssl.hwcdn.net"));
                return 0 <= b && 8 >
                    b
            }(this) && (this.inited = !0, cb.init(), window.setTimeout(function() {
                    a.imgItems.src = "img/items.png"
                }, 250), this.bitPlayer = Q.getBitmapData("img/visual.png"), this.bitBuffs = Q.getBitmapData("img/buffs.png"), l.init(), A.init(), S.setup(), this.initFont(), this.createScreen(this.getStageWidth(), this.getStageHeight()), this.createBz(), this.addChild(this.display = new kb(this.screen)), this.nodes = new V, this.player = new ma, this.player.name = "Player", this.nodes.add(new T), this.addEventListener("enterFrame", F(this, this.onFrame)),
                this.addEventListener("rightMouseDown", F(this, this.onRightPressed)), this.addEventListener("rightMouseUp", F(this, this.onRightReleased)), this.addEventListener("middleMouseDown", F(this, this.onMiddlePressed)), this.addEventListener("middleMouseUp", F(this, this.onMiddleReleased)), this.addEventListener("mouseDown", F(this, this.onLeftPressed)), this.addEventListener("mouseUp", F(this, this.onLeftReleased)), this.addEventListener("mouseMove", F(this, this.onHover)), this.addEventListener("mouseWheel", F(this, this.onWheel)),
                window.addEventListener("contextmenu", function(a) {
                    a.pageX < window.innerWidth - 168 && a.pageY < window.innerHeight - 98 && a.preventDefault()
                }), this.time = n.getTimer() / 1E3, this.updateLang())
        },
        updateLang: function() {
            Ja.confirmText = l.loc("misc", "confirmText", Ja.confirmTextEn);
            pa.get_inst().updateLang();
            X.get_inst().updateLang();
            this.nodes.updateLang()
        },
        updateBMF: function() {
            pa.get_inst().updateBMF();
            X.get_inst().updateBMF();
            null != I.onRender_lbTooltip && I.onRender_lbTooltip.updateBMF();
            this.nodes.updateBMF()
        },
        onRender: function() {
            this.clear(this.screen,
                -12562320);
            var a = this.tsavrPos;
            if (this.tapi) {
                var b = 1 - a * a;
                a = -4 - 4 * Math.cos(40 * a) * a
            } else b = 1 - a * a, a = -4 - 4 * Math.cos(40 * a);
            this.draw(this.tsavrBit, this.screen.component.width - (this.tsavrBit.component.width * b | 0), this.screen.component.height - (this.tsavrBit.component.height + a | 0));
            this.nodes.render(0, 0);
            var c = this.mouseOver;
            if (null != c && null != c.tooltipText && "" != c.tooltipText) {
                b = I.onRender_lbTooltip;
                null == b && (b = new u, b.x = 16, b.y = -8, b.set_style(1), I.onRender_lbTooltip = b);
                a = c.tooltipIcon;
                b.set_text(c.tooltipText);
                c = this.posX;
                var d = this.posY;
                this.xrect(c + 14, d - 10, b.width + 5 + (0 != a ? 23 : 0), b.height + 3, 0, .6);
                0 != a && (this.context.drawImage(this.imgItems, 40 * (a & 31), 40 * (a >> 5), 40, 40, c + b.x, d + b.y, 20, 20), c += 23);
                b.render(c, d)
            }
        },
        onFrame: function(a) {
            a = n.getTimer() / 1E3;
            this.delta = a - this.time;
            this.time = a;
            this.tsavrOn && (this.tsavrPos = Math.max(this.tsavrPos - this.delta / 3.7, 0));
            null != this.holdNode && (this.nodes.hitTest(this.posX, this.posY) == this.holdNode ? (this.holdTime = (n.getTimer() - this.holdSince) / 1E3, this.holdNode.hold(this.holdTime)) :
                (this.holdNode.released(), this.holdNode = null));
            this.nodes.update(this.delta);
            this.onRender();
            this.nodes.system()
        },
        onHover: function(a) {
            a = this.posX = B["int"](this.get_mouseX());
            var b = this.posY = B["int"](this.get_mouseY());
            this.nodes.hover(a, b);
            this.mouseOver = this.nodes.hitTest(a, b)
        },
        onWheel: function(a) {
            null != this.mouseOver && this.mouseOver.wheel(0 < a.delta ? 1 : 0 > a.delta ? -1 : 0)
        },
        doPressed: function(a) {
            if (!(this.isPressed || n.getTimer() < this.ignoreMouseEventsUntil)) {
                this.isPressed = !0;
                a = this.posX = B["int"](this.get_mouseX());
                var b = this.posY = B["int"](this.get_mouseY());
                this.holdNode = this.nodes.hitTest(a, b);
                this.holdSince = n.getTimer()
            }
        },
        onLeftPressed: function(a) {
            this.shiftDown = a.shiftKey;
            this.ctrlDown = a.ctrlKey;
            this.altDown = a.altKey;
            this.doPressed(a)
        },
        onRightPressed: function(a) {
            this.shiftDown = !a.shiftKey;
            this.ctrlDown = a.ctrlKey;
            this.doPressed(a)
        },
        onMiddlePressed: function(a) {
            this.shiftDown = a.shiftKey;
            this.ctrlDown = !a.ctrlKey;
            this.doPressed(a)
        },
        doReleased: function(a) {
            this.isPressed && (this.isPressed = !1, null != this.holdNode &&
                (this.holdNode.released(), this.holdNode = null), a = this.nodes.hitTest(B["int"](this.get_mouseX()), B["int"](this.get_mouseY())), null != a && a.click())
        },
        onLeftReleased: function(a) {
            this.shiftDown = a.shiftKey;
            this.ctrlDown = a.ctrlKey;
            this.doReleased(a)
        },
        onRightReleased: function(a) {
            this.shiftDown = !a.shiftKey;
            this.ctrlDown = a.ctrlKey;
            this.doReleased(a)
        },
        onMiddleReleased: function(a) {
            this.shiftDown = a.shiftKey;
            this.ctrlDown = !a.ctrlKey;
            this.doReleased(a)
        },
        imax: function(a, b) {
            return a > b ? a : b
        },
        textWidth: function(a) {
            this.context.font =
                u.canvasFont;
            return Math.ceil(this.context.measureText(a).width)
        },
        blitText: function(a, b, c, d, e, f, g) {
            var m = this.context;
            m.save();
            m.font = u.canvasFont;
            m.textAlign = 2 == d ? "right" : 1 == d ? "center" : "left";
            m.textBaseline = 2 == e ? "bottom" : 1 == e ? "middle" : "top";
            m.shadowColor = "black";
            m.fillStyle = this.fontFillColors[f - 1];
            m.strokeStyle = "black";
            m.lineWidth = 1;
            null != g && (m.globalAlpha = g);
            for (d = 0; d < a.length;) f = a[d], ++d, m.shadowBlur = 4, m.strokeText(f, b, c + 2 * (1 - e)), m.shadowBlur = 0, m.fillText(f, b, c + 2 * (1 - e)), c += u.canvasLineHeight;
            m.restore()
        },
        blit: function(a, b, c) {
            this._point.x = b;
            this._point.y = c;
            this._rect.x = this._rect.y = 0;
            this._rect.width = a.component.width;
            this._rect.height = a.component.height;
            this.screen.copyPixels(a, this._rect, this._point, null, null, !0)
        },
        draw: function(a, b, c, d) {
            null == d && (d = 1);
            this._offset.tx = b;
            this._offset.ty = c;
            this._alpha.alphaMultiplier = d;
            this.screen.draw(a, this._offset, 1 > d ? this._alpha : null)
        },
        drawPart: function(a, b, c, d, e, f, g, m) {
            null == m && (m = 1);
            var x = this.screen.context,
                C = x.globalAlpha;
            x.globalAlpha *= m;
            x.drawImage(a.component,
                b, c, d, e, f, g, d, e);
            x.globalAlpha = C
        },
        clear: function(a, b) {
            null == b && (b = 0);
            if (255 == b >>> 24) {
                var c = this._gfx;
                c.clear();
                c.beginFill(b & 16777215);
                c.drawRect(0, 0, a.component.width, a.component.height);
                c.endFill();
                a.draw(this._shape)
            } else this._rect.x = this._rect.y = 0, this._rect.width = a.component.width, this._rect.height = a.component.height, a.fillRect(this._rect, b)
        },
        rect: function(a, b, c, d, e) {
            this._rect.x = a;
            this._rect.y = b;
            this._rect.width = c;
            this._rect.height = d;
            this.screen.fillRect(this._rect, e | -16777216)
        },
        xrect: function(a,
            b, c, d, e, f) {
            null == f && (f = 1);
            this._gfx.clear();
            this._gfx.beginFill(e, f);
            this._gfx.drawRect(a, b, c, d);
            this._gfx.endFill();
            this.screen.draw(this._shape)
        },
        onAdded: function(a) {
            this.removeEventListener("addedToStage", F(this, this.onAdded));
            this.get_stage().addEventListener("resize", F(this, this.onResize));
            this.init()
        },
        __class__: I
    });
    var Gb = function() {
        I.call(this)
    };
    h.DocumentClass = Gb;
    Gb.__name__ = !0;
    Gb.__super__ = I;
    Gb.prototype = w(I.prototype, {
        get_stage: function() {
            return n.get_current().get_stage()
        },
        __class__: Gb
    });
    var Jb = function() {};
    h["openfl.AssetLibrary"] = Jb;
    Jb.__name__ = !0;
    Jb.prototype = {
        exists: function(a, b) {
            return !1
        },
        getBitmapData: function(a) {
            return null
        },
        __class__: Jb
    };
    var Kb = function() {
        this.type = new Y;
        this.path = new Y;
        this.add("img/overlay.png", G.IMAGE);
        this.add("img/buffs.png", G.IMAGE);
        this.add("img/color.png", G.IMAGE);
        this.add("img/nitems.png", G.IMAGE);
        this.add("img/og.png", G.IMAGE);
        this.add("img/shadow.png", G.IMAGE);
        this.add("img/side.png", G.IMAGE);
        this.add("img/visual.png", G.IMAGE)
    };
    h.DefaultAssetLibrary =
        Kb;
    Kb.__name__ = !0;
    Kb.__super__ = Jb;
    Kb.prototype = w(Jb.prototype, {
        add: function(a, b, c) {
            this.type.set(a, b);
            this.path.set(a, null != c ? c : a)
        },
        exists: function(a, b) {
            a = this.type.get(a);
            if (null != a) {
                if (a == b || null == b) return !0;
                switch (b[1]) {
                    case 5:
                        return a == G.MUSIC;
                    case 4:
                        return a == G.SOUND;
                    case 0:
                        return !0
                }
            }
            return !1
        },
        getBitmapData: function(a) {
            a = this.path.get(a);
            return t.loaders.get(a).contentLoaderInfo.content.bitmapData
        },
        __class__: Kb
    });
    var Wa = function(a, b) {
        b = b.split("u").join("");
        this.r = new RegExp(a, b)
    };
    h.EReg = Wa;
    Wa.__name__ = !0;
    Wa.prototype = {
        match: function(a) {
            this.r.global && (this.r.lastIndex = 0);
            this.r.m = this.r.exec(a);
            this.r.s = a;
            return null != this.r.m
        },
        matched: function(a) {
            if (null != this.r.m && 0 <= a && a < this.r.m.length) return this.r.m[a];
            throw new z("EReg::matched");
        },
        matchedPos: function() {
            if (null == this.r.m) throw new z("No string matched");
            return {
                pos: this.r.m.index,
                len: this.r.m[0].length
            }
        },
        matchSub: function(a, b, c) {
            null == c && (c = -1);
            if (this.r.global) {
                this.r.lastIndex = b;
                this.r.m = this.r.exec(0 > c ? a : y.substr(a, 0, b + c));
                if (b = null != this.r.m) this.r.s = a;
                return b
            }
            if (c = this.match(0 > c ? y.substr(a, b, null) : y.substr(a, b, c))) this.r.s = a, this.r.m.index += b;
            return c
        },
        replace: function(a, b) {
            return a.replace(this.r, b)
        },
        map: function(a, b) {
            var c = 0,
                d = new pc;
            do {
                if (c >= a.length) break;
                else if (!this.matchSub(a, c)) {
                    d.add(y.substr(a, c, null));
                    break
                }
                var e = this.matchedPos();
                d.add(y.substr(a, c, e.pos - c));
                d.add(b(this));
                0 == e.len ? (d.add(y.substr(a, e.pos, 1)), c = e.pos + 1) : c = e.pos + e.len
            } while (this.r.global);
            !this.r.global && 0 < c && c < a.length && d.add(y.substr(a,
                c, null));
            return d.b
        },
        __class__: Wa
    };
    var y = function() {};
    h.HxOverrides = y;
    y.__name__ = !0;
    y.cca = function(a, b) {
        a = a.charCodeAt(b);
        if (a == a) return a
    };
    y.substr = function(a, b, c) {
        if (null != b && 0 != b && null != c && 0 > c) return "";
        null == c && (c = a.length);
        0 > b ? (b = a.length + b, 0 > b && (b = 0)) : 0 > c && (c = a.length + c - b);
        return a.substr(b, c)
    };
    y.indexOf = function(a, b, c) {
        var d = a.length;
        0 > c && (c += d, 0 > c && (c = 0));
        for (; c < d;) {
            if (a[c] === b) return c;
            c++
        }
        return -1
    };
    y.remove = function(a, b) {
        b = y.indexOf(a, b, 0);
        if (-1 == b) return !1;
        a.splice(b, 1);
        return !0
    };
    y.iter =
        function(a) {
            return {
                cur: 0,
                arr: a,
                hasNext: function() {
                    return this.cur < this.arr.length
                },
                next: function() {
                    return this.arr[this.cur++]
                }
            }
        };
    var Xa = function(a) {
        this.lines = [];
        this.item = a
    };
    h["terra.ItemMeta"] = Xa;
    Xa.__name__ = !0;
    Xa.parse = function(a, b, c) {
        var d = b.split("|"),
            e = new Xa(a),
            f = d.shift();
        0 <= f.indexOf("t") && e.add(k.Tile);
        0 <= f.indexOf("w") && e.add(k.Wall);
        0 <= f.indexOf("4") && e.add(k.Helmet);
        0 <= f.indexOf("5") && e.add(k.Shirt);
        0 <= f.indexOf("6") && e.add(k.Pants);
        0 <= f.indexOf("a") && e.add(k.Accessory);
        0 <= f.indexOf("1") &&
            (e.stack = 1);
        0 <= f.indexOf("H") && (e.stack = 99);
        0 <= f.indexOf("K") && (e.stack = 999);
        for (var g = null, m = 4, x = 0, C = 0; C < d.length;) {
            var h = d[C];
            ++C;
            var l = h.indexOf("="),
                N = y.substr(h, l + 1, null);
            switch (y.substr(h, 0, l)) {
                case "d":
                    h = B.parseInt(N);
                    l = null;
                    if (0 <= b.indexOf("|t="))
                        for (N = 0; N < d.length;) {
                            var q = d[N];
                            ++N;
                            if (D.startsWith(q, "t=")) {
                                N = B.parseInt(q.substring(2));
                                null != N && 0 < N && (l = Math.round(60 * h / N));
                                break
                            }
                        }
                    0 <= f.indexOf("d") && e.add(k.MeleeDamage(h, l));
                    0 <= f.indexOf("r") && e.add(k.RangedDamage(h, l));
                    0 <= f.indexOf("m") && e.add(k.MagicDamage(h,
                        l));
                    0 <= f.indexOf("s") ? e.add(k.SummonDamage(h, l)) : e.add(k.Crit(m));
                    break;
                case "s":
                    e.stack = B.parseInt(N);
                    break;
                case "z":
                    x = B.parseInt(N);
                    break;
                case "c":
                    m += B.parseInt(N);
                    break;
                case "D":
                    e.add(k.Defense(B.parseInt(N)));
                    break;
                case "tp":
                    e.add(k.PickaxePower(B.parseInt(N)));
                    break;
                case "tx":
                    e.add(k.AxePower(5 * B.parseInt(N)));
                    break;
                case "th":
                    e.add(k.HammerPower(B.parseInt(N)));
                    break;
                case "tf":
                    e.add(k.FishingPower(B.parseInt(N)));
                    break;
                case "tr":
                    e.add(k.RangeDelta(B.parseInt(N)));
                    break;
                case "hl":
                    e.add(k.RestoresLife(B.parseInt(N)));
                    break;
                case "hm":
                    e.add(k.RestoresMana(B.parseInt(N)));
                    break;
                case "rg":
                    h = B.parseInt(N);
                    db.goalPerItem.h[a.id] = h;
                    h;
                    break;
                case "t":
                    h = B.parseInt(N);
                    e.add(k.UseTime(h, 0 < h ? "" + (6E3 / h | 0) / 100 : null));
                    break;
                case "k":
                    e.add(k.Knockback(parseFloat(N)));
                    break;
                case "m":
                    e.add(k.ManaCost(B.parseInt(N)));
                    break;
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                    g = null != g ? g + ("\n" + N) : N
            }
        }
        0 <= f.indexOf("c") && e.add(k.Consumable);
        0 <= f.indexOf("v") && e.add(k.Vanity);
        0 <= f.indexOf("b") && e.add(k.Ammo);
        0 <= f.indexOf("x") && e.add(k.Material);
        null != g && e.add(k.EnText(g));
        1 < e.stack && c && e.add(k.MaxStack(e.stack));
        c && 0 < x && (a = "", x = x / 5 | 0, 0 < x && (0 != x % 100 && (a = "[$c]" + x % 100 + " " + a), x = x / 100 | 0, 0 < x && (0 != x % 100 && (a = "[$s]" + x % 100 + " " + a), x = x / 100 | 0, 0 < x && (0 != x % 100 && (a = "[$g]" + x % 100 + " " + a), x = x / 100 | 0, 0 < x && (a = "[$p]" + x + " " + a)))), e.add(k.Worth(y.substr(a, 0, a.length - 1))));
        return e
    };
    Xa.initLang = function() {
        var a = new Xa(null);
        a.lines = [k.Tile, k.Wall, k.Helmet, k.Shirt, k.Pants, k.Accessory, k.MeleeDamage(1, 1), k.RangedDamage(1, 1), k.MagicDamage(1, 1), k.SummonDamage(1, 1), k.Crit(1),
            k.Defense(1), k.PickaxePower(1), k.AxePower(1), k.HammerPower(1), k.RangeDelta(1), k.FishingPower(1), k.RestoresLife(1), k.RestoresMana(1), k.UseTime(8, "?"), k.UseTime(20, "?"), k.UseTime(25, "?"), k.UseTime(30, "?"), k.UseTime(35, "?"), k.UseTime(45, "?"), k.UseTime(55, "?"), k.UseTime(65, "?"), k.Knockback(1), k.Knockback(3), k.Knockback(4), k.Knockback(6), k.Knockback(7), k.Knockback(9), k.Knockback(11), k.Knockback(12), k.ManaCost(1), k.Consumable, k.Vanity, k.Ammo, k.Material, k.MaxStack(1)
        ];
        a.toString()
    };
    Xa.prototype = {
        toString: function(a) {
            for (var b = [], c = function(a, c) {
                    b.push(l.loc("meta.item", a, c))
                }, d = function(a, c, d) {
                    a = D.replace(l.loc("meta.item", a, c), "$1", B.string(d));
                    b.push(a)
                }, e = function(a, c, d, e) {
                    a = D.replace(l.loc("meta.item", a, c), "$1", "" + d);
                    null != e && (a += D.replace(l.loc("meta.item", "damagePerSecond", " (~$1 DPS)"), "$1", "" + e));
                    b.push(a)
                }, f = 0, g = this.lines; f < g.length;) {
                var m = g[f];
                ++f;
                if (null == a || a(m)) switch (m[1]) {
                    case 0:
                        c("isTile", "Can be placed (tile)");
                        break;
                    case 1:
                        c("isWall", "Can be placed (wall)");
                        break;
                    case 2:
                        c("isHelmet", "Equipable (head slot)");
                        break;
                    case 3:
                        c("isShirt", "Equipable (body slot)");
                        break;
                    case 4:
                        c("isPants", "Equipable (legs slot)");
                        break;
                    case 5:
                        c("isAccessory", "Equipable (accessory)");
                        break;
                    case 10:
                        d("critChance", "$1% critical strike chance", "" + m[2]);
                        break;
                    case 6:
                        e("meleeDamage", "$1 melee damage", m[2], m[3]);
                        break;
                    case 7:
                        e("rangedDamage", "$1 ranged damage", m[2], m[3]);
                        break;
                    case 8:
                        e("magicDamage", "$1 magic damage", m[2], m[3]);
                        break;
                    case 9:
                        e("summonDamage", "$1 summon damage", m[2], m[3]);
                        break;
                    case 11:
                        d("defense", "$1 defense", m[2]);
                        break;
                    case 12:
                        d("pickaxePower", "$1% pickaxe power", m[2]);
                        break;
                    case 13:
                        d("axePower", "$1% axe power", m[2]);
                        break;
                    case 14:
                        d("hammerPower", "$1% hammer power", m[2]);
                        break;
                    case 16:
                        d("fishingPower", "$1% fishing power", m[2]);
                        break;
                    case 15:
                        var x = m[2];
                        d("rangeDelta", "$1 range", 0 > x ? "" + x : "+" + x);
                        break;
                    case 17:
                        d("restoresLife", "Restores $1 life", m[2]);
                        break;
                    case 18:
                        d("restoresMana", "Restores $1 mana", m[2]);
                        break;
                    case 19:
                        x = m[3];
                        var C = m[2];
                        m = D.replace(l.loc("meta.item", "useTime", "Use time $1"), "$1", "" + C);
                        if (null !=
                            x) {
                            var h = l.loc("meta.item", "useTimeClass", " ($1/s, $2)");
                            C = 8 >= C ? l.loc("meta.item", "useTime.insanelyFast", "insanely fast") : 20 >= C ? l.loc("meta.item", "useTime.veryFast", "very fast") : 25 >= C ? l.loc("meta.item", "useTime.fast", "fast") : 30 >= C ? l.loc("meta.item", "useTime.average", "average") : 35 >= C ? l.loc("meta.item", "useTime.slow", "slow") : 45 >= C ? l.loc("meta.item", "useTime.verySlow", "very slow") : 55 >= C ? l.loc("meta.item", "useTime.extremelySlow", "extremely slow") : l.loc("meta.item", "useTime.insanelySlow", "insanely slow");
                            m += D.replace(D.replace(h, "$1", x), "$2", C)
                        }
                        b.push(m);
                        break;
                    case 20:
                        x = m[2];
                        m = l.loc("meta.item", "knockback", "Knockback $1 ($2)");
                        h = 1.5 >= x ? l.loc("meta.item", "knockback.extremelyWeak", "extremely weak") : 3 >= x ? l.loc("meta.item", "knockback.veryWeak", "very weak") : 4 >= x ? l.loc("meta.item", "knockback.weak", "weak") : 6 >= x ? l.loc("meta.item", "knockback.average", "average") : 7 >= x ? l.loc("meta.item", "knockback.strong", "strong") : 9 >= x ? l.loc("meta.item", "knockback.veryStrong", "very strong") : 11 >= x ? l.loc("meta.item", "knockback.extremelyStrong",
                            "extremely strong") : l.loc("meta.item", "knockback.insane", "insane");
                        b.push(D.replace(D.replace(m, "$1", "" + x), "$2", h));
                        break;
                    case 22:
                        c("isConsumable", "Consumable");
                        break;
                    case 23:
                        c("isVanity", "Vanity item");
                        break;
                    case 24:
                        c("isAmmo", "Ammo");
                        break;
                    case 25:
                        c("isMaterial", "Material");
                        break;
                    case 27:
                        d("maxStack", "Max stack $1", m[2]);
                        break;
                    case 28:
                        d("worth", "Worth $1", m[2]);
                        break;
                    case 21:
                        d("manaCost", "Uses $1 mana", m[2]);
                        break;
                    case 29:
                        d("researchGoal", "Takes $1 to research in Journey Mode", m[2]);
                        break;
                    case 26:
                        m =
                            x = m[2], null != this.item.pid && (m = l.iloc("ItemTooltip", this.item.pid, x)), b.push(m)
                }
            }
            return b.join("\n")
        },
        add: function(a) {
            this.lines.push(a)
        },
        __class__: Xa
    };
    var k = h["terra.ItemMetaLine"] = {
        __ename__: !0,
        __constructs__: "Tile Wall Helmet Shirt Pants Accessory MeleeDamage RangedDamage MagicDamage SummonDamage Crit Defense PickaxePower AxePower HammerPower RangeDelta FishingPower RestoresLife RestoresMana UseTime Knockback ManaCost Consumable Vanity Ammo Material EnText MaxStack Worth ResearchGoal".split(" ")
    };
    k.Tile = ["Tile", 0];
    k.Tile.toString = v;
    k.Tile.__enum__ = k;
    k.Wall = ["Wall", 1];
    k.Wall.toString = v;
    k.Wall.__enum__ = k;
    k.Helmet = ["Helmet", 2];
    k.Helmet.toString = v;
    k.Helmet.__enum__ = k;
    k.Shirt = ["Shirt", 3];
    k.Shirt.toString = v;
    k.Shirt.__enum__ = k;
    k.Pants = ["Pants", 4];
    k.Pants.toString = v;
    k.Pants.__enum__ = k;
    k.Accessory = ["Accessory", 5];
    k.Accessory.toString = v;
    k.Accessory.__enum__ = k;
    k.MeleeDamage = function(a, b) {
        a = ["MeleeDamage", 6, a, b];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.RangedDamage = function(a, b) {
        a = ["RangedDamage", 7, a, b];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.MagicDamage = function(a, b) {
        a = ["MagicDamage", 8, a, b];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.SummonDamage = function(a, b) {
        a = ["SummonDamage", 9, a, b];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.Crit = function(a) {
        a = ["Crit", 10, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.Defense = function(a) {
        a = ["Defense", 11, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.PickaxePower = function(a) {
        a = ["PickaxePower", 12, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.AxePower = function(a) {
        a = ["AxePower", 13, a];
        a.__enum__ =
            k;
        a.toString = v;
        return a
    };
    k.HammerPower = function(a) {
        a = ["HammerPower", 14, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.RangeDelta = function(a) {
        a = ["RangeDelta", 15, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.FishingPower = function(a) {
        a = ["FishingPower", 16, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.RestoresLife = function(a) {
        a = ["RestoresLife", 17, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.RestoresMana = function(a) {
        a = ["RestoresMana", 18, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.UseTime = function(a, b) {
        a = ["UseTime", 19, a, b];
        a.__enum__ =
            k;
        a.toString = v;
        return a
    };
    k.Knockback = function(a) {
        a = ["Knockback", 20, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.ManaCost = function(a) {
        a = ["ManaCost", 21, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.Consumable = ["Consumable", 22];
    k.Consumable.toString = v;
    k.Consumable.__enum__ = k;
    k.Vanity = ["Vanity", 23];
    k.Vanity.toString = v;
    k.Vanity.__enum__ = k;
    k.Ammo = ["Ammo", 24];
    k.Ammo.toString = v;
    k.Ammo.__enum__ = k;
    k.Material = ["Material", 25];
    k.Material.toString = v;
    k.Material.__enum__ = k;
    k.EnText = function(a) {
        a = ["EnText", 26, a];
        a.__enum__ =
            k;
        a.toString = v;
        return a
    };
    k.MaxStack = function(a) {
        a = ["MaxStack", 27, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.Worth = function(a) {
        a = ["Worth", 28, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    k.ResearchGoal = function(a) {
        a = ["ResearchGoal", 29, a];
        a.__enum__ = k;
        a.toString = v;
        return a
    };
    var l = function() {};
    h.Lang = l;
    l.__name__ = !0;
    l.initDefLang = function() {
        var a = {};
        l.defLang = a;
        Xa.initLang();
        a.misc = {
            name: "English",
            useBitmapFont: !0,
            itemLocale: "en-US"
        };
        return a
    };
    l.capitalize = function(a) {
        return a.charAt(0).toUpperCase() + y.substr(a,
            1, null)
    };
    l.makeID = function(a) {
        a = (new Wa("[^\\w]+", "g")).replace(a, "");
        return a.charAt(0).toLowerCase() + y.substr(a, 1, null)
    };
    l.loc = function(a, b, c) {
        if (l.isDebug) return "" + a + "/" + b;
        var d = l.curLang;
        if (null != d) {
            if (a = d[a], null != a && (b = a[b], null != b)) return b
        } else if (null != c) {
            d = l.defLang[a];
            if (null == d) {
                var e = d = {};
                l.defLang[a] = e
            }
            d[b] = c
        }
        return c
    };
    l.loc1 = function(a, b, c, d) {
        a = l.loc(a, b, c);
        return l.isDebug ? "" + a + "(" + d + ")" : D.replace(a, "$1", d)
    };
    l.iloc = function(a, b, c) {
        if (l.isDebug) return "@" + a + "/" + b;
        var d = l.itemsJSON,
            e =
            null;
        null != d && (a = d[a], null != a && (e = a[b]));
        null == e && (e = c);
        if (null != e)
            for (b = 0; 8 > b && (b++, c = [!1], e = l.rxIVar.map(e, function(a) {
                    return function(b) {
                        var c = b.matched(1);
                        b = b.matched(2);
                        var e = null != d ? d[c] : null;
                        if (null != e && (e = e[b], null != e)) return 0 <= e.indexOf("{$") && (a[0] = !0), e;
                        e = l.itemsJSONen;
                        null == e && (e = l.zip.getJSON("Terraria.Localization.Content.en-US.Items.json"), l.itemsJSONen = e);
                        return null != e && (e = e[c], null != e && (e = e[b], null != e)) ? (0 <= e.indexOf("{$") && (a[0] = !0), e) : b
                    }
                }(c)), c[0]););
        return e
    };
    l.ttip = function(a,
        b, c) {
        return l.isDebug ? "" + a + "/" + b + ".ttip" : l.loc(a, b + ".ttip", c)
    };
    l.getDefLang = Zc.getLang = function() {
        return l.defLang
    };
    l.detectBMFont = function() {
        if (l.noBMFont) return !1;
        if (null == l.curLang) return !0;
        var a = l.curLang.misc;
        return null != a ? !!a.useBitmapFont : !1
    };
    l.setLang = function(a, b) {
        if (null == a || "string" == typeof a) {
            var c = a;
            a = null
        } else c = "custom";
        l.langCode = c;
        "debug" == c ? (c = null, l.isDebug = !0) : l.isDebug = !1;
        null != c ? (l.curLang = null != a ? a : l.zip.getJSON("Terrasavr." + c + ".json"), c = l.loc("misc", "itemLocale", "en-US"),
            l.itemsJSON = l.zip.getJSON("Terraria.Localization.Content." + c + ".Items.json"), "en-EN" == c && (l.itemsJSONen = l.itemsJSON)) : (l.itemsJSON = null, l.curLang = null);
        c = 0;
        for (a = A.list; c < a.length;) {
            var d = a[c];
            ++c;
            d.updateLang()
        }
        c = 0;
        for (a = wa.pairDefs; c < a.length;) d = a[c], ++c, d.updateLang();
        c = 0;
        for (a = wa.list; c < a.length;) d = a[c], ++c, d.updateLang();
        null == b && (b = l.detectBMFont());
        l.noBMFont && (b = !1);
        b != u.useBMFont && (u.useBMFont = b, I._main.updateBMF());
        I._main.updateLang();
        T.inst.btLang.set_text(l.loc("misc", "langName", "English"))
    };
    l.init = function() {
        var a = window.localStorage;
        if (null != a) {
            l.noBMFont = "true" == a.getItem("terrasavr.useSystemFont");
            l.useCustomFont = "true" == a.getItem("terrasavr.useCustomFont");
            var b = window.localStorage.getItem("terrasavr.customFont");
            null != b && "" != b && (lb.inst.fdCustomFont.set_value(b), u.canvasFont = u.canvasFontPre + b)
        }
        l.zip = new qc("lang/lang.zip?v=21-06-16", function(b) {
            b.ready && (T.inst.add(T.inst.btLang), b = cb.map.get("lang"), null != b ? l.setLang(b) : null != a && (b = a.getItem("terrasavr.lang"), l.setLang(b)))
        })
    };
    var Lb = function() {
        this.length = 0
    };
    h.List = Lb;
    Lb.__name__ = !0;
    Lb.prototype = {
        add: function(a) {
            a = [a];
            null == this.h ? this.h = a : this.q[1] = a;
            this.q = a;
            this.length++
        },
        push: function(a) {
            this.h = a = [a, this.h];
            null == this.q && (this.q = a);
            this.length++
        },
        __class__: Lb
    };
    Math.__name__ = !0;
    var Db = function() {
        ea.call(this);
        var a = this.getBackgroundColor(),
            b = 0;
        70 > .299 * (a >> 16 & 255) + .587 * (a >> 8 & 255) + .114 * (a & 255) && (b = 16777215);
        a = this.getHeight() / 2 - 3.5;
        var c = this.getWidth() - 60;
        this.outline = new fa;
        this.outline.get_graphics().beginFill(b,
            .07);
        this.outline.get_graphics().drawRect(0, 0, c, 7);
        this.outline.set_x(30);
        this.outline.set_y(a);
        this.addChild(this.outline);
        this.progress = new fa;
        this.progress.get_graphics().beginFill(b, .35);
        this.progress.get_graphics().drawRect(0, 0, c - 4, 3);
        this.progress.set_x(32);
        this.progress.set_y(a + 2);
        this.progress.set_scaleX(0);
        this.addChild(this.progress)
    };
    h.NMEPreloader = Db;
    Db.__name__ = !0;
    Db.__super__ = fa;
    Db.prototype = w(fa.prototype, {
        getBackgroundColor: function() {
            return 4214896
        },
        getHeight: function() {
            return n.get_current().get_stage().get_stageHeight()
        },
        getWidth: function() {
            return n.get_current().get_stage().get_stageWidth()
        },
        onInit: function() {},
        onLoaded: function() {
            this.dispatchEvent(new Z("complete"))
        },
        onUpdate: function(a, b) {
            a /= b;
            1 < a && (a = 1);
            this.progress.set_scaleX(a)
        },
        __class__: Db
    });
    var oa = function() {};
    h.Reflect = oa;
    oa.__name__ = !0;
    oa.field = function(a, b) {
        try {
            return a[b]
        } catch (c) {
            return c instanceof z && (c = c.val), null
        }
    };
    oa.fields = function(a) {
        var b = [];
        if (null != a) {
            var c = Object.prototype.hasOwnProperty,
                d;
            for (d in a) "__id__" != d && "hx__closures__" != d && c.call(a,
                d) && b.push(d)
        }
        return b
    };
    oa.isFunction = function(a) {
        return "function" == typeof a && !(a.__name__ || a.__ename__)
    };
    oa.compareMethods = function(a, b) {
        return a == b ? !0 : oa.isFunction(a) && oa.isFunction(b) ? a.scope == b.scope && a.method == b.method && null != a.method : !1
    };
    var B = function() {};
    h.Std = B;
    B.__name__ = !0;
    B.string = function(a) {
        return J.__string_rec(a, "")
    };
    B["int"] = function(a) {
        return a | 0
    };
    B.parseInt = function(a) {
        var b = parseInt(a, 10);
        0 != b || 120 != y.cca(a, 1) && 88 != y.cca(a, 1) || (b = parseInt(a));
        return isNaN(b) ? null : b
    };
    var pc = function() {
        this.b =
            ""
    };
    h.StringBuf = pc;
    pc.__name__ = !0;
    pc.prototype = {
        add: function(a) {
            this.b += B.string(a)
        },
        __class__: pc
    };
    var D = function() {};
    h.StringTools = D;
    D.__name__ = !0;
    D.htmlEscape = function(a, b) {
        a = a.split("&").join("&amp;").split("<").join("&lt;").split(">").join("&gt;");
        return b ? a.split('"').join("&quot;").split("'").join("&#039;") : a
    };
    D.startsWith = function(a, b) {
        return a.length >= b.length && y.substr(a, 0, b.length) == b
    };
    D.endsWith = function(a, b) {
        var c = b.length,
            d = a.length;
        return d >= c && y.substr(a, d - c, c) == b
    };
    D.isSpace = function(a,
        b) {
        a = y.cca(a, b);
        return 8 < a && 14 > a || 32 == a
    };
    D.ltrim = function(a) {
        for (var b = a.length, c = 0; c < b && D.isSpace(a, c);) c++;
        return 0 < c ? y.substr(a, c, b - c) : a
    };
    D.rtrim = function(a) {
        for (var b = a.length, c = 0; c < b && D.isSpace(a, b - c - 1);) c++;
        return 0 < c ? y.substr(a, 0, b - c) : a
    };
    D.trim = function(a) {
        return D.ltrim(D.rtrim(a))
    };
    D.replace = function(a, b, c) {
        return a.split(b).join(c)
    };
    D.hex = function(a, b) {
        var c = "";
        do c = "0123456789ABCDEF".charAt(a & 15) + c, a >>>= 4; while (0 < a);
        if (null != b)
            for (; c.length < b;) c = "0" + c;
        return c
    };
    var rb = function() {};
    h.Type =
        rb;
    rb.__name__ = !0;
    rb.resolveClass = function(a) {
        a = h[a];
        return null != a && a.__name__ ? a : null
    };
    rb.resolveEnum = function(a) {
        a = h[a];
        return null != a && a.__ename__ ? a : null
    };
    rb.createInstance = function(a, b) {
        switch (b.length) {
            case 0:
                return new a;
            case 1:
                return new a(b[0]);
            case 2:
                return new a(b[0], b[1]);
            case 3:
                return new a(b[0], b[1], b[2]);
            case 4:
                return new a(b[0], b[1], b[2], b[3]);
            case 5:
                return new a(b[0], b[1], b[2], b[3], b[4]);
            case 6:
                return new a(b[0], b[1], b[2], b[3], b[4], b[5]);
            case 7:
                return new a(b[0], b[1], b[2], b[3], b[4],
                    b[5], b[6]);
            case 8:
                return new a(b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7]);
            default:
                throw new z("Too many arguments");
        }
    };
    var Ea = function() {
        this.tooltipIcon = 0;
        this.tooltipText = this.tooltipEnText = null;
        this.x = this.y = 0;
        this.m = I._main
    };
    h["dom.Node"] = Ea;
    Ea.__name__ = !0;
    Ea.prototype = {
        update: function(a) {},
        render: function(a, b) {},
        hitTest: function(a, b) {
            return null
        },
        click: function() {},
        hold: function(a) {},
        released: function() {},
        hover: function(a, b) {},
        wheel: function(a) {},
        system: function() {},
        updateLang: function() {},
        updateBMF: function() {},
        __class__: Ea
    };
    var V = function() {
        Ea.call(this);
        this.nodes = [];
        this.waitAdd = [];
        this.waitRem = []
    };
    h["dom.Container"] = V;
    V.__name__ = !0;
    V.__super__ = Ea;
    V.prototype = w(Ea.prototype, {
        add: function(a) {
            this.waitAdd.push(a)
        },
        remove: function(a) {
            this.waitRem.push(a)
        },
        system: function() {
            var a;
            var b = this.waitAdd.length;
            var c = this.waitRem.length;
            for (a = b; 0 <= --a;) {
                var d = this.waitAdd[a];
                for (b = c; 0 <= --b;) this.waitRem[b] == d && (y.remove(this.waitRem, this.waitRem[b]), y.remove(this.waitAdd, d))
            }
            b = this.waitRem.length;
            for (a = 0; a < b;) y.remove(this.nodes,
                this.waitRem[a]), a++;
            for (; 0 <= --b;) this.waitRem.pop();
            b = this.waitAdd.length;
            for (a = 0; a < b;) this.nodes.push(this.waitAdd[a]), a++;
            for (; 0 <= --b;) this.waitAdd.pop();
            b = this.nodes.length;
            for (a = 0; a < b;) this.nodes[a].system(), a++
        },
        hitTest: function(a, b) {
            for (var c = this.nodes.length, d; 0 <= --c;)
                if (null != (d = this.nodes[c].hitTest(a - this.x, b - this.y))) return d;
            return null
        },
        update: function(a) {
            for (var b = -1, c = this.nodes.length; ++b < c;) this.nodes[b].update(a)
        },
        render: function(a, b) {
            for (var c = -1, d = this.nodes.length; ++c < d;) this.nodes[c].render(this.x +
                a, this.y + b)
        },
        hover: function(a, b) {
            for (var c = -1, d = this.nodes.length; ++c < d;) this.nodes[c].hover(a - this.x, b - this.y)
        },
        updateLang: function() {
            for (var a = -1, b = this.nodes.length; ++a < b;) this.nodes[a].updateLang();
            b = this.waitAdd.length;
            for (a = -1; ++a < b;) this.waitAdd[a].updateLang()
        },
        updateBMF: function() {
            for (var a = -1, b = this.nodes.length; ++a < b;) this.nodes[a].updateBMF();
            b = this.waitAdd.length;
            for (a = -1; ++a < b;) this.waitAdd[a].updateBMF()
        },
        __class__: V
    });
    var la = function(a, b) {
        this.cachedTime = -1;
        V.call(this);
        this.fixed =
            b;
        this.slot = a;
        this.label = new u;
        this.label.set_halign(this.label.set_valign(2));
        this.label.x = 37;
        this.label.y = 50;
        b && this.set_buff(new S);
        this.add(this.label)
    };
    h["app.BuffNode"] = la;
    la.__name__ = !0;
    la.__super__ = V;
    la.prototype = w(V.prototype, {
        clear: function() {
            this.set_id(this.set_time(0))
        },
        copy: function(a) {
            this.set_id(a.get_id());
            this.set_time(a.get_time())
        },
        swap: function(a) {
            var b = this.get_id();
            this.set_id(a.get_id());
            a.set_id(b);
            b = this.get_time();
            this.set_time(a.get_time());
            a.set_time(b)
        },
        hitTest: function(a,
            b) {
            return a >= this.x && b >= this.y && a < this.x + 36 && b < this.y + 36 ? this : null
        },
        click: function() {
            if (this.m.ctrlDown) this.buff.time = S.getMaxTime();
            else {
                var a = la.inHand;
                null != a ? (la.inHand = null, a.inter(this)) : 0 != this.get_id() && (la.inHand = this);
                L.buff = this
            }
        },
        inter: function(a) {
            var b = L.temp;
            this != a && (this.fixed || a.fixed ? this.fixed && !a.fixed ? 0 != a.get_id() ? (this != b ? (b.copy(a), a.copy(this)) : b.swap(a), b.click()) : a.copy(this) : !this.fixed && a.fixed && this.clear() : 0 != a.get_id() ? (b.copy(a), a.copy(this), b.click(), this.clear()) :
                a.swap(this))
        },
        drawBuff: function(a, b, c) {
            var d = this.get_id();
            var calImg = window.__calamityBuffImg && window.__calamityBuffImg[d];
            if (calImg) {
                var calGa = this.m.context.globalAlpha;
                this.m.context.globalAlpha = c;
                window.__calamityDrawIcon(this.m.context, calImg, a - 2, b - 2);
                this.m.context.globalAlpha = calGa
            } else {
                if (0 > d || d >= S.COUNT) d = 0;
                this.m.drawPart(this.m.bitBuffs, 40 * (d & 31), 40 * (d >> 5), 40, 40, a - 2, b - 2, c)
            }
        },
        render: function(a, b) {
            var c = this.get_id();
            var d = la.inHand == this ? .6 : 0 != c ? 1 : .6;
            10 <= this.slot && 77 > this.m.player.version ? d *= .5 : 22 <= this.slot && 269 > this.m.player.version && (d *= .5);
            this.get_id() > la.maxId && (d *= .7);
            this.drawBuff(a + this.x, b + this.y, d);
            this.cachedTime != this.get_time() && (this.cachedTime = this.get_time(), this.label.set_text(this.buff.getTime()));
            0 < c && !this.fixed && V.prototype.render.call(this, a, b)
        },
        set_buff: function(a) {
            this.buff != a && (this.buff = a, this.label.set_text(a.getTime()));
            return a
        },
        get_id: function() {
            return this.buff.id
        },
        set_id: function(a) {
            return this.buff.id = a
        },
        get_time: function() {
            return this.buff.time | 0
        },
        set_time: function(a) {
            this.buff.time != a && (this.buff.time = a, this.label.set_text(this.buff.getTime()));
            return a
        },
        __class__: la
    });
    var Hc = function() {};
    h["app.Libraries"] = Hc;
    Hc.__name__ = !0;
    Hc.deploy = function() {
        var a = function(a, b, c) {
                null ==
                    c && (c = 0);
                return new ub(c, a, b)
            },
            b = function(a, b, c) {
                null == c && (c = 0);
                0 == c && (0 != b[0] ? c = b[0] : 0 != b[10] && (c = b[10]));
                return new mb(c, a, b)
            },
            c = function(c, d, e) {
                for (var f = [], g = 0, m = A.list; g < m.length;) {
                    var h = m[g];
                    ++g;
                    e(h) && f.push(h.id)
                }
                e = f.length;
                c = 42 == y.cca(c, c.length - 1) ? c.substring(0, c.length - 1) : c + (" (" + e + ")");
                if (40 >= e) return b(c, f, d);
                if (480 >= e) {
                    g = [];
                    h = null;
                    for (var k = 0; k < e;) {
                        var l = k++;
                        0 == l % 40 && (h = [], m = b("Page " + ((l / 40 | 0) + 1), h, f[l]), g.push(m));
                        h.push(f[l])
                    }
                    return a(c, g, d)
                }
                if (4800 >= e) {
                    g = [];
                    m = null;
                    l = null;
                    for (h = 0; h <
                        e;) {
                        var q = h++;
                        k = f[q];
                        0 == q % 40 && (q = q / 40 | 0, 0 == q % 10 && (m = [], l = a("Pages " + (q + 1) + "+", m, k), g.push(l)), l = [], q = b("Page " + (q + 1), l, k), m.push(q));
                        l.push(k)
                    }
                    return a(c, g, d)
                }
                throw new z("Too many items (" + e + ")");
            },
            d;
        var e = a("", []);
        e.nodes.push(a("Materials", [a("Pre-Hardmode", [b("Copper & Tin", [12, 3507, 3509, 89, 0, 699, 3501, 3503, 687, 0, 20, 3508, 3505, 80, 0, 703, 3502, 3499, 688, 0, 145, 3504, 3506, 76, 0, 717, 3498, 3500, 689, 0, 146, 106, 15, 0, 0, 720, 710, 707, 0, 0], 20), b("Iron & Lead", [11, 6, 1, 90, 0, 700, 3495, 3497, 690, 0, 22, 4, 7, 81, 0, 704, 3496,
            3493, 691, 0, 3951, 99, 10, 77, 1140, 3953, 3492, 3494, 692, 1139, 3952, 0, 35, 954, 4424, 3954, 0, 716, 0, 1448
        ], 22), b("Silver & Tungsten", [14, 3513, 3515, 91, 0, 701, 3489, 3491, 693, 0, 21, 3514, 3511, 82, 0, 705, 3490, 3487, 694, 0, 143, 3510, 3512, 78, 0, 718, 3486, 3488, 695, 0, 144, 107, 16, 278, 0, 721, 711, 708, 4915, 0], 21), b("Gold & Platinum", [13, 3519, 3521, 92, 264, 702, 3483, 3485, 696, 715, 19, 3520, 3517, 83, 349, 706, 3484, 3481, 697, 714, 141, 3516, 3518, 79, 105, 719, 3480, 3482, 698, 713, 142, 108, 17, 955, 0, 722, 712, 709, 0, 0], 19), b("Demonite & Crimtane", [56, 46, 103, 102, 956,
            880, 795, 798, 792, 0, 57, 0, 104, 101, 957, 1257, 801, 797, 793, 0, 86, 44, 45, 100, 958, 1329, 796, 799, 794, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        ], 57), b("Hellstone & Obsidian", [174, 3697, 231, 122, 4536, 0, 1458, 2642, 2657, 0, 175, 2618, 232, 217, 4535, 1460, 2600, 2651, 2667, 2662, 192, 173, 233, 120, 4534, 1461, 2406, 2644, 2380, 2840, 330, 1457, 3067, 214, 4533, 1459, 1473, 2390, 1463, 4110], 175), b("Meteorite", [116, 3702, 123, 204, 197, 0, 3129, 3138, 3177, 0, 117, 3180, 124, 234, 127, 3153, 3126, 3171, 3168, 3159, 3100, 0, 125, 0, 0, 3156, 3150, 3135, 3141, 3147, 3101, 3144, 0, 0, 0, 3174, 3162, 3132,
            3165, 4141
        ], 117), b("Other", [331, 190, 228, 3347, 3374, 3266, 960, 956, 0, 0, 209, 0, 229, 3348, 3375, 3267, 961, 957, 0, 0, 210, 191, 230, 3380, 3376, 3268, 962, 958, 0, 0, 1310, 0, 185, 4059, 0, 0, 0, 0, 0, 0], 331)], 20), a("Hardmode", [b("Cobalt & Palladium", [364, 483, 776, 373, 371, 1104, 1185, 1188, 1207, 1206, 381, 537, 385, 372, 960, 1184, 1186, 1189, 1205, 0, 415, 435, 991, 374, 961, 1589, 1187, 1222, 1208, 0, 420, 0, 383, 375, 962, 1590, 0, 1190, 1209, 0], 381), b("Mythril & Orichalcum", [365, 484, 777, 378, 376, 1105, 1192, 1195, 1211, 1212, 382, 390, 386, 377, 0, 1191, 1193, 1196, 1210, 0,
                416, 436, 992, 379, 0, 0, 1194, 1223, 1213, 0, 421, 525, 384, 380, 0, 0, 1220, 1197, 1214, 0
            ], 382), b("Adamantite & Titanium", [366, 482, 778, 401, 400, 1106, 1199, 1202, 1216, 1217, 391, 406, 388, 402, 0, 1198, 1200, 1203, 1215, 0, 604, 481, 993, 403, 0, 1593, 1201, 1224, 1218, 0, 605, 524, 387, 404, 0, 1594, 1221, 1204, 1219, 0], 391), b("Hallowed & Chlorophyte", [1225, 368, 990, 553, 558, 947, 1227, 1230, 1002, 1003, 4896, 550, 579, 559, 4897, 1006, 1228, 1231, 1001, 2792, 4900, 578, 367, 551, 4898, 1235, 1229, 1233, 1004, 1234, 4901, 4790, 4873, 552, 4899, 1179, 1226, 1232, 1005, 1262], 1225),
            b("Shroomite & Ectoplasm", [183, 2176, 1547, 1546, 0, 1508, 1506, 1543, 1503, 2189, 1552, 0, 1548, 1549, 0, 3261, 1507, 1544, 1504, 0, 2791, 0, 0, 1550, 0, 0, 0, 1545, 1505, 0, 2794, 0, 0, 1866, 0, 0, 0, 0, 823, 0], 1552), b("Frost, Turtle & Beetle", [724, 725, 496, 684, 1253, 0, 1316, 0, 2199, 0, 1306, 670, 1572, 685, 0, 0, 1317, 0, 2200, 2201, 676, 0, 1264, 686, 2161, 0, 1318, 0, 2202, 2280, 0, 0, 726, 822, 1519, 0, 1328, 0, 2218, 0], 2218), b("Luminite & Martian", [2860, 3701, 2806, 2803, 3460, 0, 2815, 2820, 2813, 0, 2861, 2814, 2807, 2804, 3467, 2824, 2809, 2818, 2825, 2810, 0, 0, 2808, 2805, 3461,
                2826, 2823, 2819, 2821, 2855, 0, 2822, 3567, 3568, 3472, 2812, 2811, 2816, 2817, 4121
            ], 3467), b("Solar", [3458, 4164, 2763, 2786, 3543, 0, 4155, 4157, 4152, 3539, 0, 4153, 2764, 2784, 3473, 4162, 4154, 4150, 4149, 4145, 3573, 4229, 2765, 3522, 2856, 4163, 4161, 4156, 4158, 4160, 4233, 4159, 3468, 0, 2858, 4151, 4146, 4148, 4147, 4165]), b("Vortex", [3456, 4185, 2757, 2776, 3475, 0, 4176, 4178, 4173, 3536, 0, 4174, 2758, 2774, 0, 4183, 4175, 4171, 4170, 4166, 3574, 4230, 2759, 3523, 0, 4184, 4182, 4177, 4179, 4181, 4234, 4180, 3469, 0, 0, 4172, 4167, 4169, 4168, 4186]), b("Stardust", [3459, 4227,
                3381, 3466, 3474, 0, 4218, 4220, 4215, 3538, 0, 4216, 3382, 3464, 3531, 4225, 4217, 4213, 4212, 4208, 3576, 4232, 3383, 3462, 3465, 4226, 4224, 4219, 4221, 4223, 4236, 4222, 3471, 3463, 3525, 4214, 4209, 4211, 4210, 4228
            ], 3459), b("Nebula", [3457, 4206, 2760, 2781, 0, 0, 4197, 4199, 4194, 3537, 0, 4195, 2761, 2779, 0, 4204, 4196, 4192, 4191, 4187, 3575, 4231, 2762, 3524, 3476, 4205, 4203, 4198, 4200, 4202, 4235, 4201, 3470, 0, 3542, 4193, 4188, 4190, 4189, 4207], 3457)
        ], 1191), a("Non-ore", [b("Granite", [3086, 3703, 0, 0, 0, 0, 3131, 3140, 3179, 0, 3088, 3125, 0, 0, 0, 3155, 3128, 3173, 3170, 3161,
            3087, 4719, 0, 0, 0, 3158, 3152, 3137, 3143, 3149, 3089, 3146, 0, 0, 0, 3176, 3164, 3134, 3167, 4122
        ], 3087), b("Marble", [3081, 3704, 0, 0, 0, 0, 3130, 3139, 3178, 0, 3082, 3181, 0, 0, 0, 3154, 3127, 3172, 3169, 3160, 3066, 4554, 0, 0, 0, 3157, 3151, 3136, 3142, 3148, 3083, 3145, 0, 0, 0, 3175, 3163, 3133, 3166, 4123], 3066), b("Crystal", [502, 3886, 4982, 3283, 3051, 0, 3888, 3891, 3894, 0, 0, 3884, 4983, 518, 495, 3920, 3898, 3890, 3893, 3895, 3234, 0, 4984, 494, 0, 3909, 3918, 3892, 3915, 3896, 3238, 3903, 0, 515, 3009, 3889, 3897, 3911, 3917, 4124], 502), b("Sandstone", [3271, 4268, 607, 3339, 0, 2120,
            4307, 4309, 4305, 0, 3273, 4267, 608, 3346, 0, 4314, 4306, 4303, 4302, 4298, 4051, 4720, 3276, 3277, 0, 4315, 4313, 4308, 4310, 4312, 4053, 4311, 3344, 3345, 0, 4304, 4299, 4301, 4300, 4316
        ], 607), b("Lesion", [61, 3976, 0, 0, 0, 0, 3967, 3970, 3964, 0, 4486, 3965, 0, 0, 0, 3974, 3966, 3962, 3961, 3958, 3955, 996, 609, 0, 0, 3975, 3973, 3969, 3971, 3972, 3956, 3957, 610, 0, 0, 3963, 3959, 3968, 3960, 4126], 3965), b("Ice", [664, 3672, 684, 676, 974, 3048, 2044, 2040, 2059, 0, 2009, 681, 685, 4911, 0, 2248, 2594, 2049, 2100, 2076, 883, 593, 686, 594, 0, 2252, 2635, 2086, 2247, 2848, 884, 3908, 822, 595, 2198,
            2288, 2068, 3913, 2031, 4111
        ]), b("Glass", [170, 3700, 2862, 1272, 2244, 2243, 1709, 2037, 2065, 0, 392, 2748, 1271, 4260, 0, 1713, 2239, 2048, 2097, 2075, 0, 2194, 1268, 1270, 0, 2632, 2414, 2085, 2254, 2842, 0, 1702, 1269, 1267, 0, 1703, 1719, 2639, 2025, 4112], 170), b("Skyware", [824, 3674, 0, 0, 0, 0, 837, 2042, 2063, 0, 825, 838, 0, 0, 0, 830, 2606, 2053, 2102, 2080, 751, 765, 0, 0, 0, 2631, 2410, 2090, 2384, 2834, 752, 2628, 0, 0, 0, 826, 2070, 2394, 2029, 4104], 824), b("Lihzahrd", [1101, 3677, 1148, 1152, 0, 0, 1137, 2041, 2062, 0, 1102, 1142, 1149, 1153, 0, 1144, 2595, 2052, 2101, 2079, 0, 2195,
            1147, 1154, 0, 1145, 2416, 2089, 2385, 2836, 0, 3906, 1146, 0, 0, 1143, 2069, 2396, 2030, 4106
        ]), b("Golden", [141, 3887, 0, 0, 0, 0, 1710, 2147, 2143, 0, 142, 3885, 0, 0, 0, 1716, 2238, 2155, 2151, 2663, 0, 0, 0, 0, 0, 3910, 2405, 2133, 2379, 2843, 0, 3904, 0, 0, 0, 1704, 1720, 2389, 2137, 1705]), b("Steampunk", [1344, 3686, 839, 1742, 0, 0, 1712, 2036, 2655, 0, 3751, 2250, 840, 0, 0, 1718, 2241, 2649, 2096, 2125, 0, 2203, 841, 0, 0, 2253, 2412, 2130, 2256, 2845, 0, 2627, 948, 4472, 0, 1708, 1722, 2638, 2024, 4114]), b("Blue dungeon", [134, 3693, 0, 0, 0, 0, 1411, 1408, 2652, 0, 135, 2614, 0, 0, 0, 1397, 3900,
            1405, 2664, 2658, 0, 1384, 0, 0, 0, 1398, 2402, 2645, 2376, 2837, 0, 4238, 0, 0, 0, 1396, 1470, 2386, 1414, 4107
        ]), b("Green dungeon", [137, 3691, 0, 0, 0, 0, 1412, 1409, 2653, 0, 138, 2612, 0, 0, 0, 1400, 3901, 1406, 2665, 2659, 0, 1386, 0, 0, 0, 1401, 2403, 2646, 2377, 2838, 0, 4239, 0, 0, 0, 1399, 1471, 2387, 1415, 4108]), b("Pink dungeon", [139, 3692, 0, 0, 0, 0, 1413, 1410, 2654, 0, 140, 2613, 0, 0, 0, 1403, 3902, 1407, 2666, 2660, 0, 1385, 0, 0, 0, 1404, 2404, 2647, 2378, 2839, 0, 4240, 0, 0, 0, 1402, 1472, 2388, 1416, 4109])], 1344), a("Wood", [b("Forest wood", [9, 3665, 727, 24, 0, 335, 25, 136, 108, 0,
            93, 48, 728, 196, 0, 32, 359, 105, 349, 336, 480, 1447, 729, 39, 0, 36, 2397, 341, 333, 2827, 0, 94, 0, 0, 0, 34, 224, 334, 354, 358
        ], 9), b("Mahogany", [620, 3669, 733, 656, 0, 0, 651, 2038, 2060, 0, 623, 626, 734, 657, 0, 639, 2597, 2050, 2098, 2077, 4718, 2211, 735, 658, 0, 636, 2399, 2087, 642, 2829, 0, 632, 3360, 3361, 0, 629, 645, 648, 2026, 4097]), b("Pearlwood", [621, 3670, 736, 659, 0, 0, 652, 2039, 2061, 0, 624, 627, 737, 660, 0, 640, 2602, 2051, 2099, 2078, 0, 2212, 738, 661, 0, 637, 2400, 2088, 643, 2830, 0, 633, 0, 0, 0, 630, 646, 649, 2027, 4098]), b("Ebonwood", [619, 3668, 730, 653, 0, 0, 650, 2033, 2056,
            0, 622, 625, 731, 654, 0, 638, 2593, 2046, 2093, 2073, 0, 2210, 732, 655, 0, 635, 2398, 2083, 641, 2828, 0, 631, 0, 0, 0, 628, 644, 647, 2021, 4096
        ]), b("Shadewood", [911, 3675, 924, 921, 0, 0, 912, 2146, 2142, 0, 927, 914, 925, 922, 0, 917, 2604, 2154, 2150, 2127, 0, 2213, 926, 923, 0, 916, 2401, 2132, 919, 2835, 0, 913, 0, 0, 0, 915, 920, 918, 2136, 4105]), b("Boreal wood", [2503, 3689, 2509, 2745, 0, 0, 2561, 2564, 2558, 0, 2505, 2559, 2510, 2746, 0, 677, 2560, 2556, 2555, 2552, 4717, 2507, 2511, 2747, 0, 673, 858, 2563, 2565, 2852, 0, 2566, 0, 0, 0, 2557, 2553, 2562, 2554, 4119], 2503), b("Palm wood", [2504,
            3687, 2512, 2517, 0, 2521, 2528, 2530, 2525, 0, 2506, 2526, 2513, 2516, 0, 2532, 2601, 2523, 2522, 2519, 0, 2508, 2514, 2515, 0, 2534, 2527, 2533, 2531, 2850, 0, 2518, 0, 0, 0, 2524, 2520, 2529, 2536, 4118
        ], 2504), b("Living wood", [832, 3673, 0, 0, 0, 0, 819, 2145, 2141, 0, 1723, 831, 0, 0, 0, 829, 2596, 2153, 2149, 2126, 0, 2196, 0, 0, 0, 2633, 2636, 2131, 2245, 2833, 0, 2629, 0, 0, 0, 806, 2139, 3914, 2135, 4099], 832), b("Dynasty", [2260, 3684, 0, 0, 2235, 0, 2265, 2226, 2224, 0, 2263, 2230, 0, 0, 2234, 2259, 2237, 2236, 2227, 2232, 2264, 0, 0, 0, 2261, 2229, 3919, 2225, 3916, 2849, 0, 3905, 0, 0, 2262, 2228,
            2231, 3912, 2233, 4117
        ], 2260), b("Spooky wood", [1729, 3699, 1832, 0, 0, 0, 1815, 2043, 2064, 0, 1730, 2620, 1833, 0, 0, 1816, 2605, 2650, 2103, 2081, 0, 0, 1834, 0, 0, 1817, 2409, 2091, 2383, 2847, 0, 1818, 0, 0, 0, 1814, 2071, 2393, 2028, 4116], 1729)], 9), a("Plants & Organic", [b("Bamboo", [4564, 4585, 0, 0, 0, 0, 4576, 4578, 4573, 0, 4565, 4574, 0, 0, 0, 4583, 4575, 4571, 4570, 4566, 4547, 4667, 0, 0, 0, 4584, 4582, 4577, 4579, 4581, 4548, 4580, 0, 0, 0, 4572, 4567, 4569, 4568, 4586], 4564), b("Cactus", [276, 3695, 894, 882, 0, 0, 816, 2032, 2055, 0, 750, 2616, 895, 881, 0, 2743, 2592, 2045, 2092, 2072,
            0, 4390, 896, 0, 0, 812, 2408, 2082, 2382, 2854, 0, 2744, 0, 0, 0, 807, 2066, 2392, 2020, 4100
        ], 276), b("Pumpkin", [1725, 3698, 1731, 1754, 0, 0, 1793, 2641, 2656, 0, 1726, 2619, 1732, 1755, 0, 1794, 2603, 2054, 2668, 2661, 0, 0, 1733, 1756, 0, 1795, 2415, 2643, 2671, 2846, 0, 1796, 0, 0, 0, 1792, 2669, 2637, 2670, 4115], 1725), b("Mushroom", [183, 3688, 756, 0, 4779, 2539, 818, 2546, 2543, 4921, 194, 2544, 787, 0, 4780, 2550, 2599, 2542, 2541, 2537, 0, 4721, 0, 0, 4781, 814, 2413, 2547, 2548, 2851, 764, 2549, 1181, 0, 0, 810, 2538, 2545, 2540, 4103], 183), b("Bone", [154, 3694, 151, 959, 0, 3724, 820, 2148,
            2144, 0, 932, 2615, 152, 1166, 0, 827, 2591, 3004, 2152, 2128, 766, 0, 153, 1320, 0, 811, 2407, 2134, 2381, 2831, 768, 634, 0, 3003, 786, 808, 2140, 2391, 2138, 4101
        ], 766), b("Honey", [1125, 3685, 2361, 1123, 1121, 2257, 1711, 2035, 2058, 0, 1127, 2249, 2362, 2364, 1132, 1717, 2240, 2648, 2095, 2124, 1128, 2204, 2363, 2888, 2787, 2251, 2411, 2129, 2255, 2844, 1134, 2630, 2502, 1130, 2788, 1707, 1721, 2395, 2023, 4113], 1125), b("Slime", [23, 3690, 1309, 2585, 0, 1683, 2576, 2579, 2573, 0, 3111, 2574, 2610, 2493, 4959, 2583, 2575, 2571, 2570, 2567, 762, 767, 3113, 3090, 0, 815, 2582, 2578, 2580, 2853,
            769, 2581, 0, 0, 0, 2572, 2568, 2577, 2569, 4120
        ], 762), b("Flesh", [836, 3696, 996, 0, 0, 0, 817, 2034, 2057, 0, 4509, 2617, 2193, 0, 0, 828, 2598, 2047, 2094, 2074, 763, 0, 4050, 0, 0, 813, 2634, 2084, 2246, 2832, 770, 3907, 4052, 0, 0, 809, 2067, 2640, 2022, 4102], 763), b("Spider", [2607, 3950, 2370, 2551, 0, 0, 3941, 3943, 3938, 0, 4503, 3939, 2371, 2366, 0, 3948, 3940, 3936, 3935, 3931, 4139, 0, 2372, 0, 0, 3949, 3947, 3942, 3944, 3946, 4140, 3945, 1798, 0, 0, 3937, 3932, 3934, 3933, 4125], 2607)], 1125)], 21));
        e.nodes.push(a("Decorative", [a("Furniture", [b("Chair", [34, 628, 629, 630, 806,
            807, 808, 809, 810, 826, 915, 1143, 1396, 1399, 1402, 1459, 1509, 1703, 1704, 1707, 1708, 1792, 1814, 1925, 2228, 2288, 2524, 2557, 2572, 2812, 3174, 4572, 3176, 3889, 3937, 3963, 4151, 4172, 4193, 4214
        ]), b("Workbench", [36, 635, 636, 637, 673, 811, 812, 4584, 814, 815, 916, 1145, 1398, 1401, 1404, 1461, 1511, 1795, 1817, 2172, 2229, 2251, 2252, 2253, 2534, 2631, 2632, 2633, 2826, 3156, 3157, 3158, 3909, 3910, 3949, 3975, 4163, 4184, 4205, 4226]), b("Table", [32, 638, 639, 640, 677, 827, 3974, 829, 830, 917, 1144, 1397, 1400, 1403, 1460, 1510, 1713, 1714, 1716, 1717, 1718, 1794, 1816, 4583, 2248,
            2259, 2532, 4314, 2583, 2743, 2824, 3153, 3154, 3155, 3920, 3948, 4162, 4183, 4204, 4225
        ]), b("Bed", [224, 644, 645, 646, 920, 1470, 1471, 1472, 1473, 1719, 1720, 1721, 1722, 2066, 2067, 2068, 2069, 2070, 2071, 2139, 2140, 2231, 2520, 2538, 2553, 2568, 2669, 2811, 3162, 3163, 3164, 3897, 3932, 3959, 4146, 4167, 4188, 4209, 4299, 4567]), b("Lantern", [136, 344, 347, 1390, 1391, 1392, 1393, 1808, 2032, 2033, 2034, 2035, 2036, 2037, 2038, 2039, 2040, 2041, 2042, 2043, 2145, 2146, 2147, 2148, 2226, 2530, 2546, 2564, 2579, 2641, 2642, 2820, 3138, 3139, 3140, 3891, 4157, 4178, 4199, 4220]), b("Lamp",
            [341, 1394, 2082, 2083, 2084, 2085, 2086, 2087, 2088, 2089, 2090, 2091, 2129, 2130, 2131, 2132, 2133, 2134, 2225, 2533, 2547, 2563, 2578, 2643, 2644, 2645, 2646, 2647, 2819, 4577, 3135, 3136, 3137, 3969, 4156, 4177, 4198, 4219, 3892, 3942], 341), b("Torch", [8, 342, 427, 428, 429, 430, 431, 432, 433, 523, 974, 1245, 1333, 2274, 3004, 3045, 3114, 4383, 4384, 4385, 4386, 4387, 4388, 966, 3046, 3047, 3048, 3049, 3050, 3723, 3724, 4689, 4690, 4691, 4692, 4693, 4694, 0, 0, 0], 8), b("Candle", [105, 713, 1405, 1406, 1407, 2045, 2046, 2047, 2048, 2049, 2050, 2051, 2052, 2053, 2054, 2153, 2154, 2155, 2236,
            2523, 2542, 2556, 2571, 2648, 2649, 2650, 2651, 2818, 3171, 3172, 3173, 3890, 3936, 3962, 4150, 4171, 4192, 4213, 4303, 4571
        ]), b("Candelabra", [349, 714, 2092, 2093, 2094, 2095, 2096, 2097, 2098, 2099, 2100, 2101, 2102, 2103, 2149, 2150, 2151, 2152, 2522, 2541, 2555, 2570, 2664, 2665, 2666, 2667, 2668, 3168, 3169, 3170, 3893, 3935, 3961, 4149, 4170, 4191, 4212, 4302, 4570, 2825]), b("Chandelier", [106, 107, 108, 712, 2055, 2056, 2057, 2058, 2059, 2060, 2656, 2573, 2061, 2062, 2063, 2064, 2065, 2141, 2142, 2143, 2144, 2525, 2543, 2558, 2652, 2653, 2654, 2655, 2657, 2813, 3178, 3177, 3179, 3894,
            3938, 3964, 4152, 4173, 4215, 4194
        ]), b("Door", [25, 650, 651, 652, 816, 4307, 818, 819, 820, 837, 912, 1137, 1138, 1139, 1140, 1411, 1412, 1413, 1458, 1709, 1710, 1711, 1712, 1793, 1815, 1924, 2044, 2265, 2528, 2561, 2576, 2815, 3129, 3130, 3131, 4415, 3888, 3941, 3967, 4576])], 34), a("Furniture (cont.)", [b("Bookcase", [354, 1414, 1415, 1416, 1463, 1512, 4568, 2021, 2022, 2023, 2024, 2025, 2026, 2027, 2028, 2029, 2030, 2031, 2135, 2136, 2137, 2138, 2233, 2536, 2540, 2554, 2569, 2670, 2817, 3165, 3166, 3167, 3917, 3933, 3960, 4147, 4168, 4189, 4210, 4300]), b("Sofa", [858, 2397, 2398, 2399,
            2400, 2401, 2402, 2403, 2404, 2405, 2406, 2407, 2408, 2409, 2410, 2411, 2412, 2413, 2414, 2415, 2416, 2527, 2582, 2634, 2635, 2636, 2823, 3150, 3151, 3152, 3918, 3919, 3947, 3973, 4161, 4182, 4203, 4224, 4313, 4582
        ]), b("Dresser", [334, 647, 648, 649, 918, 2386, 2387, 2388, 2389, 2390, 2391, 2392, 2393, 2394, 2395, 2396, 2529, 2545, 2562, 2577, 2637, 2638, 2639, 2640, 2816, 3132, 3133, 3134, 3911, 3912, 3913, 3914, 3934, 3968, 4148, 4169, 4190, 4211, 4301, 4569]), b("Piano", [333, 641, 642, 643, 919, 2245, 2246, 2247, 2254, 2255, 2256, 2376, 2377, 2378, 2379, 2380, 2381, 2382, 2383, 2384, 2385,
            2531, 2548, 2565, 2580, 2671, 2821, 3141, 3142, 3143, 3915, 3916, 3944, 3971, 4158, 4179, 4200, 4221, 4310, 4579
        ]), b("Bathtub", [336, 2072, 2073, 2074, 2075, 2076, 2077, 2078, 2079, 2080, 2081, 2124, 2125, 2126, 2127, 2128, 2232, 2519, 2537, 2552, 2567, 2658, 2659, 2660, 2661, 2662, 2663, 2810, 3159, 3160, 3161, 3895, 3931, 3958, 4145, 4166, 4187, 4208, 4298, 4566]), b("Sink", [2827, 2828, 2829, 2830, 2831, 2832, 2833, 2834, 2835, 2836, 2837, 2838, 2839, 2840, 2841, 2842, 2843, 2844, 2845, 2846, 2847, 2848, 2849, 2850, 2851, 2852, 2853, 2854, 2855, 3147, 3148, 3149, 3896, 3946, 3972, 4160,
            4181, 4202, 4223, 4581
        ]), b("Toilet", [358, 1705, 4096, 4097, 4098, 4099, 4100, 4101, 4102, 4103, 4104, 4105, 4106, 4107, 4108, 4109, 4110, 4111, 4112, 4113, 4114, 4115, 4116, 4117, 4118, 4119, 4120, 4121, 4122, 4123, 4124, 4125, 4126, 4586, 4141, 4165, 4186, 4207, 4228, 4316])], 354), b("Paintings 1", [1372, 1373, 1374, 1375, 1419, 1420, 1421, 1422, 1423, 1424, 1425, 1426, 1427, 1428, 1433, 1434, 1435, 1436, 1437, 1438, 1439, 1440, 1441, 1442, 1443, 1476, 1477, 1478, 1479, 1480, 1481, 1482, 1483, 1484, 1485, 1486, 1487, 1488, 1489, 1490], 1427), b("Paintings 2", [1491, 1492, 1493, 1494,
            1495, 1496, 1497, 1498, 1499, 1500, 1501, 1502, 1538, 1539, 1540, 1541, 1542, 1573, 1574, 1575, 1576, 1577, 1846, 1847, 1848, 1849, 1850, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        ], 1496)], 354));
        e.nodes.push(a("Pets, mounts, tools", [b("Pets", [1242, 4551, 669, 4550, 4736, 1810, 603, 3628, 4735, 4365, 1311, 4604, 4605, 4603, 1172, 2587, 1180, 4701, 1927, 4737, 4425, 1799, 4366, 1171, 2420, 2338, 1312, 0, 0, 0, 1181, 4777, 0, 0, 0, 115, 3062, 3043, 425, 1183], 603), b("Boss pets", [994, 3060, 1959, 1170, 1169, 4799, 3857, 3855, 1182, 1798, 4815, 4816, 4803, 4813, 4802, 4814, 4817, 4805, 4810, 4809,
            4806, 4804, 4801, 4797, 4960, 4800, 4798, 4808, 0, 0, 1837, 0, 0, 0, 0, 4811, 4807, 4812, 3856, 3577
        ], 1170), c("Mounts*", 2430, function(a) {
            if (4444 == a.id) return !0;
            a = a.textLq;
            return 0 <= a.indexOf("summons") && (0 <= a.indexOf("rideable") || 0 <= a.indexOf("mount"))
        }), b("Hooks", [84, 437, 1236, 1237, 1238, 1239, 1240, 1241, 1800, 1829, 1915, 1916, 2360, 2585, 2800, 3020, 3021, 3022, 3023, 3572, 3623, 4257, 4759, 4980, 939, 1273, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]), b("Minecarts", [2343, 4066, 4067, 4426, 4427, 4428, 4429, 4450, 4451, 4452, 4453, 4454, 4455, 4456, 4467, 4468, 4469,
            4472, 4745, 4470, 4471, 4763, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        ])], 603));
        e.nodes.push(d = c("Potions (regeneration)*", 499, function(a) {
            a = a.nameLq;
            return D.endsWith(a, "potion") && (0 <= a.indexOf("regeneration") || 0 <= a.indexOf("healing") || 0 <= a.indexOf("mana") || 0 <= a.indexOf("restoration"))
        }));
        e.nodes.push(c("Potions (effects)*", 296, function(a) {
            var b = a.nameLq;
            a = a.id;
            return 678 != a && D.endsWith(b, "potion") && 0 > y.indexOf(d.nodes, a, 0)
        }));
        e.nodes.push(b("Bosses & events", [43, 1133, 560, 602, 0, 557, 556, 544, 1315, 3828, 70, 1331,
            1307, 361, 0, 1293, 1844, 2767, 1958, 3816, 267, 0, 0, 0, 0, 2673, 4988, 4961, 3601, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        ], 43));
        e.nodes.push(b("Quest fish", [2475, 2476, 2450, 2477, 2478, 2451, 2479, 2480, 2452, 2453, 2481, 2454, 2482, 2483, 2455, 2456, 2457, 2458, 2459, 2460, 2484, 2472, 2461, 2462, 2463, 2485, 2464, 2465, 2486, 2466, 2467, 2468, 2487, 2469, 2488, 2470, 2471, 2473, 2474, 4393], 2487));
        e.nodes.push(a("Categories", [a("Weapons", [c("Melee damage", 3514, function(a) {
                return 0 <= a.metatype.indexOf("d")
            }), c("Ranged damage", 3510, function(a) {
                return 0 <= a.metatype.indexOf("r")
            }),
            c("Magic damage", 113, function(a) {
                return 0 <= a.metatype.indexOf("m")
            }), c("Summon damage", 1309, function(a) {
                return 0 <= a.metatype.indexOf("s") && !(0 <= a.metatype.indexOf("S")) && !(0 <= a.pid.indexOf("Whip"))
            }), c("Summoner whips", 4672, function(a) {
                return 0 <= a.metatype.indexOf("s") && 0 <= a.pid.indexOf("Whip")
            }), c("Sentry damage", 1572, function(a) {
                return 0 <= a.metatype.indexOf("S")
            })
        ], 204), a("Equipable", [c("Armor", 81, function(a) {
                a = a.metatype;
                return !(0 <= a.indexOf("v")) && (0 <= a.indexOf("4") || 0 <= a.indexOf("5") || 0 <= a.indexOf("6"))
            }),
            c("Accessories", 158, function(a) {
                a = a.metatype;
                return 0 <= a.indexOf("a") && !(0 <= a.indexOf("v"))
            }), c("Vanity", 239, function(a) {
                return 0 <= a.metatype.indexOf("v")
            }), c("Head slot", 92, function(a) {
                return 0 <= a.metatype.indexOf("4")
            }), c("Body slot", 83, function(a) {
                return 0 <= a.metatype.indexOf("5")
            }), c("Leg slot", 79, function(a) {
                return 0 <= a.metatype.indexOf("6")
            }), c("Dyes", 1009, function(a) {
                return D.endsWith(a.name, "Dye")
            }), c("Wings", 823, function(a) {
                return 0 <= a.textLq.indexOf("allows flight")
            })
        ], 90), a("Tools", [c("Pickaxes",
            3515,
            function(a) {
                return 0 <= a.metadata.indexOf("tp=")
            }), c("Axes", 3512, function(a) {
            return 0 <= a.metadata.indexOf("tx=")
        }), c("Hammers", 3511, function(a) {
            return 0 <= a.metadata.indexOf("th=")
        }), c("Fishing poles", 2295, function(a) {
            return 0 <= a.metadata.indexOf("tf=")
        })], 1), c("Placeable", 2, function(a) {
            return 0 <= a.metatype.indexOf("t")
        }), c("Walls", 30, function(a) {
            return 0 <= a.metatype.indexOf("w")
        })], 531));
        e.nodes.push(function() {
            var c = [],
                d = a("Items by ID", c, 149),
                e, x;
            for (e = 1; e < I.ITEMS;) c.push(a(e + "-" + (e + 399), [],
                e)), e += 400;
            for (e = 1; e < I.ITEMS;) {
                var h = [];
                for (x = -1; 40 > ++x;) e + x < I.ITEMS && h.push(e + x);
                c[e / 400 | 0].nodes.push(b("" + e + "-" + (e + 39), h, e));
                e += 40
            }
            return d
        }());
        if (window.__calamityBuildLibraryNode) try {
            var calNode = window.__calamityBuildLibraryNode();
            calNode && e.nodes.push(calNode)
        } catch (calEx) {
            console.error("[calamity] library node build failed", calEx)
        }
        return e
    };
    var O = function(a, b, c) {
        null == b && (b = 0);
        this.color = -1;
        this.kind = 0;
        this.finder = a;
        this.kind = b;
        this.meta = "";
        V.call(this);
        this._count = -1;
        this.fdCount = new u;
        this.fdCount.x = 38;
        this.fdCount.y = 42;
        this.fdCount.set_halign(2);
        this.fdCount.set_valign(2);
        this.fdCount.set_text("0");
        this.add(this.fdCount)
    };
    h["app.SlotNode"] = O;
    O.__name__ = !0;
    O.bestPrefixByPid = null;
    fetch("prefix_best.json").then(function(a) {
        return a.json()
    }).then(function(a) {
        O.bestPrefixByPid = a;
        console.log("[tsx] prefix_best.json loaded, " + Object.keys(a).length + " entries")
    })["catch"](function(a) {
        console.log("[tsx] failed to load prefix_best.json", a)
    });
    O.applyBestPrefix = function(a) {
        if (null == O.bestPrefixByPid) return void console.log("[tsx] applyBestPrefix: table not loaded yet");
        if (null == a.slot.item || 0 == a.slot.item.id) return;
        var b = a.slot.item.pid;
        null == b && (b = A.id2pid.h[a.slot.item.id]);
        if (null == b) return void console.log("[tsx] applyBestPrefix: no pid for id " + a.slot.item.id);
        var c = O.bestPrefixByPid[b];
        if (null == c) return void console.log("[tsx] applyBestPrefix: no entry for pid " + b);
        for (var d in A.prefixes.h)
            if (A.prefixes.h[d].enName === c) {
                a.set_prefix(parseInt(d));
                console.log("[tsx] applied " + c + " to " + b);
                return
            } console.log("[tsx] applyBestPrefix: prefix name not found in A.prefixes: " + c)
    };
    O.applyBestPrefixNative = O.applyBestPrefix;
    O.applyBestPrefix = function(a) {
        if (window.__calamityApplyBestPrefixToLibrarySlot && window.__calamityApplyBestPrefixToLibrarySlot(a)) return;
        O.applyBestPrefixNative(a)
    };
    O.__super__ = V;
    O.prototype =
        w(V.prototype, {
            hitTest: function(a, b) {
                var c = null;
                a -= this.x;
                b -= this.y;
                null == c && 0 <= a && 0 <= b && 40 > a && 40 > b && (c = this);
                return c
            },
            released: function() {
                var a = O.inHand;
                null != a ? (O.inHand = null, a.inter(this)) : A.isEmpty(this.slot.item) || (this.m.ctrlDown ? this.set_count(this.slot.item.stack) : this.m.shiftDown && 1 < this.get_count() ? (a = X.tempItem, a.copy(this), a.set_count(this.get_count() >> 1), this.set_count(this.get_count() - a.get_count()), O.inHand = a) : O.inHand = this);
                X.set_item(this)
            },
            hold: function(a) {
                1 == this.kind && this.set_count(.17 >
                    a ? this.get_count() : .27 > a ? 1 : 1.07 > a ? Math.floor((a - .27) / .08) + 1 : 2.07 > a ? Math.floor((a - 1.07) / .04) + 10 : 3.57 > a ? Math.floor((a - 2.07) / .02) + 25 : function(b) {
                        b = Math.floor((a - 3.57) / .01) + 100;
                        return 999 > b ? b : 999
                    }(this))
            },
            wheel: function(a) {
                var b = this.get_count();
                b += a;
                1 > b && (b = 1);
                this.set_count(b)
            },
            drawItem: function(a, b, c) {
                var d = this.slot.item;
                if (null != d && (0 != d.id || "" != d.name)) {
                    var e = this.m.context,
                        f = e.globalAlpha;
                    e.globalAlpha = c;
                    d.calImg ? window.__calamityDrawIcon(e, d.calImg, a, b) : e.drawImage(this.m.imgItems, d.iconX, d.iconY, 40, 40, a, b, 40, 40);
                    e.globalAlpha = f
                }
            },
            render: function(a,
                b) {
                O.current == this && this.m.rect(a + this.x + 1, b + this.y + 1, 38, 38, 1 == this.kind ? 14467228 : 10272988);
                if (1 != this.kind) {
                    var c = this.slot.item;
                    if (null != c) {
                        var d = pa.get_inst().searchResults;
                        0 > y.indexOf(d, c.id, 0) || this.m.xrect(a + this.x + 1, b + this.y + 1, 38, 38, 13693183, .8 + .2 * Math.sin(5 * this.m.time))
                    }
                }
                c = !0;
                0 == this.kind ? c = this.slot.visAvail(this.m.player) : 2 == this.kind && (c = 200 <= this.m.player.invVersion);
                d = 1 == this.kind ? 10518624 : -1 != this.color ? this.color : 6324384;
                this.m.xrect(a + this.x + 2, b + this.y + 2, 36, 36, d, c ? 1 : .5);
                c = this == O.inHand ?
                    .7 : 1;
                null != this.slot.item && this.slot.item.id > O.maxId && (c *= .5);
                this.drawItem(this.x + a, this.y + b, c);
                !A.isItem(this.slot.item) || 1 == this.kind && 1 >= this.get_count() || (d = this.get_count(), this._count != d && this.fdCount.set_text(A.count(this._count = d)), V.prototype.render.call(this, a, b))
            },
            set_id: function(a) {
                this.slot.item != a && (this.slot.item = a, O.current == this && X.get_inst().sync_id(a));
                return a
            },
            get_count: function() {
                return this.slot.count
            },
            set_count: function(a) {
                this.slot.count != a && (this.slot.count = a, O.current ==
                    this && X.get_inst().fdCount.set_value(a));
                return a
            },
            get_prefix: function() {
                return this.slot.prefix
            },
            set_prefix: function(a) {
                this.slot.prefix != a && (this.slot.prefix = a, O.current == this && X.get_inst().set_prefix(a));
                return a
            },
            clear: function() {
                this.set_id(A.fromId(0));
                this.set_count(this.set_prefix(0))
            },
            copy: function(a) {
                this.set_id(a.slot.item);
                this.set_count(a.get_count());
                this.set_prefix(a.get_prefix())
            },
            swap: function(a) {
                var b = this.slot.item;
                this.set_id(a.slot.item);
                a.set_id(b);
                b = this.get_count();
                this.set_count(a.get_count());
                a.set_count(b);
                b = this.get_prefix();
                this.set_prefix(a.get_prefix());
                a.set_prefix(b)
            },
            inter: function(a) {
                var b = X.tempItem;
                if (this != a) {
                    if (1 != this.kind && 1 != a.kind) A.isEmpty(a.slot.item) ? a.swap(this) : (a.slot.item == this.slot.item && a.get_prefix() == this.get_prefix() ? a.set_count(a.get_count() + this.get_count()) : (b.copy(a), a.copy(this), b.released()), this.clear());
                    else if (1 == this.kind && 1 != a.kind)
                        if (A.isEmpty(a.slot.item)) a.copy(this), O.applyBestPrefix(a);
                        else if (a.slot.item == this.slot.item && a.get_prefix() == this.get_prefix()) a.set_count(a.get_count() + this.get_count()), this.clear();
                    else this != b ? (b.copy(a), a.copy(this), O.applyBestPrefix(a)) : b.swap(a), b.released();
                    else 1 != this.kind && 1 == a.kind && this.clear()
                }
            },
            set_slot: function(a) {
                this.slot != a && (this.slot = a);
                return a
            },
            __class__: O
        });
    var r = function() {
        this.style = 1;
        this.title = "???";
        V.call(this)
    };
    h["app.TabBase"] = r;
    r.__name__ = !0;
    r.__super__ = V;
    r.prototype = w(V.prototype, {
        start: function() {},
        end: function() {},
        sync: function(a) {},
        addContainer: function(a, b) {
            var c = new V;
            c.x = a;
            c.y = b;
            this.add(c);
            return c
        },
        addLabel: function(a, b, c, d, e, f) {
            null == f && (f = 0);
            null == e &&
                (e = 0);
            null == d && (d = 1);
            var g = new u;
            g.x = a;
            g.y = b;
            g.set_halign(e);
            g.set_valign(f);
            g.set_style(d);
            g.set_text(g.enText = c);
            this.add(g);
            return g
        },
        addButton: function(a, b, c, d, e) {
            null == d && (d = 1);
            var f = new R;
            f.x = a;
            f.y = b;
            f.set_style(d);
            f.set_text(f.enText = c);
            f.onClick = e;
            this.add(f);
            return f
        },
        addConfirm: function(a, b, c, d, e) {
            null == d && (d = 1);
            var f = new Ja;
            f.x = a;
            f.y = b;
            f.set_style(d);
            f.set_text(f.enText = c);
            f.onClick = e;
            this.add(f);
            return f
        },
        addCheckbox: function(a, b, c, d, e) {
            null == d && (d = 1);
            var f = new Mb;
            f.x = a;
            f.y = b;
            f.set_style(d);
            f.set_format(f.enText = c);
            f.onChange = e;
            this.add(f);
            return f
        },
        addInt: function(a, b, c, d, e) {
            null == d && (d = 1);
            null == c && (c = "$");
            var f = new vb;
            f.x = a;
            f.y = b;
            f.set_format(c);
            f.set_style(d);
            f.onFinish = e;
            this.add(f);
            return f
        },
        addFloat: function(a, b, c, d, e) {
            null == d && (d = 1);
            null == c && (c = "$");
            var f = new Nb;
            f.x = a;
            f.y = b;
            f.set_format(c);
            f.set_style(d);
            f.onFinish = e;
            this.add(f);
            return f
        },
        addStr: function(a, b, c, d, e) {
            null == d && (d = 1);
            null == c && (c = "$");
            var f = new eb;
            f.x = a;
            f.y = b;
            f.set_format(c);
            f.set_style(d);
            f.onFinish = e;
            this.add(f);
            return f
        },
        addColor: function(a, b, c, d) {
            null == c && (c = 1);
            var e = new Ob;
            e.x = a;
            e.y = b;
            e.set_format("#$");
            e.set_style(c);
            e.onFinish = d;
            this.add(e);
            return e
        },
        __class__: r
    });
    var fb = function() {
        r.call(this);
        this.buffs = []
    };
    h["app.TabBuffs"] = fb;
    fb.__name__ = !0;
    fb.__super__ = r;
    fb.prototype = w(r.prototype, {
        addBuff: function(a, b, c, d) {
            null == d && (d = !1);
            c = new la(c, d);
            c.x = a;
            c.y = b;
            this.buffs.push(c);
            this.add(c);
            return c
        },
        sync: function(a) {
            var b;
            for (b = -1; ++b < this.buffs.length;) 0 <= this.buffs[b].slot && this.buffs[b].set_buff(a.buffs[b])
        },
        __class__: fb
    });
    var X = function() {
        this.lxPrefixId = -1;
        var a = this;
        r.call(this);
        X._inst = this;
        var b, c = this.m.font.size;
        X.tempItem = new O(function(a) {
            return null
        }, 1, "temp");
        X.tempItem.set_slot(new na);
        this.lbName = this.addLabel(0, 0, "Name: ", 4);
        this.fdName = this.addLabel(this.lbName.width, this.lbName.y, "", 1);
        this.lbIndex = this.addLabel(0, c, "Index: ", 4);
        this.fdIndex = this.addInt(this.lbIndex.width, c);
        this.fdIndex.onFinish = function(a) {
            O.current.set_id(A.fromId(a))
        };
        this.lbCode = this.addLabel(0, c, "Code: ", 4);
        this.fdCode =
            this.addStr(this.lbCode.width, c, '"$"', 1);
        this.fdCode.onFinish = function(a) {
            O.current.set_id(A.fromCode(a))
        };
        this.m.tapi ? (this.remove(this.lbIndex), this.remove(this.fdIndex)) : (this.remove(this.lbCode), this.remove(this.fdCode));
        this.lbCount = this.addLabel(0, 2 * c, "Count: ", 4);
        this.fdCount = this.addInt(this.lbCount.width, 2 * c);
        this.fdCount.onFinish = function(a) {
            O.current.set_count(a)
        };
        this.lbPrefix = this.addLabel(0, 3 * c, "Prefix: ", 4);
        this.fdPrefix = this.addInt(this.lbPrefix.width, 3 * c);
        this.fdPrefix.onFinish = function(b) {
            a.set_prefix(b)
        };
        this.fdPrefix.onChange = function(b) {
            a.lxPrefix.x = a.fdPrefix.x + a.fdPrefix.width
        };
        this.lxPrefix = this.addButton(this.fdPrefix.x + this.fdPrefix.width, 3 * c, "?");
        this.btCalBestPrefix = this.addButton(this.lxPrefix.x + this.lxPrefix.width + 8, 3 * c, "★", 5, function() {
            window.__calamityApplyBestPrefix && window.__calamityApplyBestPrefix(O.current, X.get_inst())
        });
        this.lxInfo = this.addLabel(20, 4 * c, "");
        this.pfxMeta = [
            [{
                name: "Best",
                nodes: [60, 59, 82, 83, 81, M.PLegendary2, M.PFabled]
            }, {
                name: "Damage",
                nodes: [20, 57, 59, 5, 81, 25, 82, 35, 83, M.PLegendary2, M.PFabled, M.PWorthy]
            }, {
                name: "Critical",
                nodes: [61, 60, 59, 44, 46, 3, 81, 16, 82, 83, M.PLegendary2]
            }],
            [{
                name: "Accessory",
                nodes: [62, 63, 67, 69, 70, 73, 74, 77, 78, 66]
            }, {
                name: "Accessory+",
                nodes: [64,
                    65, 68, 71, 72, 75, 76, 79, 80
                ]
            }, {
                name: "Universal+",
                nodes: [36, 37, 38, 53, 54, 55, 57, 59, 61]
            }, {
                name: "Common+",
                nodes: [42, 43, 44, 45, 46, 51]
            }, {
                name: "Melee+",
                nodes: [1, 2, 3, 4, 5, 6, 12, 14, 15, 81]
            }, {
                name: "Ranged+",
                nodes: [16, 17, 18, 19, 20, 21, 25, 58, 82]
            }, {
                name: "Magic+",
                nodes: [26, 27, 28, 32, 33, 34, 35, 82, 83]
            }, {
                name: "Summon+",
                nodes: [M.PFabled, M.PLoyal, M.PWorthy, M.PFocused, M.PEager, M.PBallistic, M.PScraggling, M.PPatient, M.PRabid, M.PIllTempered]
            }],
            [{
                name: "Universal-",
                nodes: [39, 40, 41, 56]
            }, {
                name: "Common-",
                nodes: [47, 48, 49, 50]
            }, {
                name: "Melee-",
                nodes: [7, 8, 9, 10, 11, 13]
            }, {
                name: "Ranged-",
                nodes: [22, 23, 24]
            }, {
                name: "Magic-",
                nodes: [29, 30, 31]
            }, {
                name: "Summon-",
                nodes: [M.PPetty, M.PFeeble, M.PSkittish]
            }]
        ];
        this.prefixes = this.pfxMeta[0];
        this.btMeta = [];
        var d = 10;
        for (b = -1; 3 > ++b;) {
            this.btMeta.push(this.addButton(d, 4 * c + 8, 1 > b ? "Library" : 2 > b ? "Positive" : "Negative", 1 > b ? 6 : 2 > b ? 4 : 2, this.getMetaClick(b)));
            d += this.btMeta[b].width + 8;
            for (var e = 0, f = this.pfxMeta[b]; e < f.length;) {
                var g = f[e];
                ++e;
                g.enName = g.name
            }
        }
        this.btPrefix = [];
        this.btGroups = [];
        this.pfxGroup = 0;
        this.btMeta[0].click();
        X.set_item(X.tempItem)
    };
    h["app.TabEdit"] = X;
    X.__name__ = !0;
    X.get_inst = function() {
        null == X._inst && new X;
        return X._inst
    };
    X.set_item = function(a) {
        if (O.current != a) {
            O.current = a;
            var b = X.get_inst();
            b.sync_id(a.slot.item);
            b.fdCount.set_value(a.get_count());
            b.fdPrefix.set_value(a.get_prefix());
            b.lxPrefixSync(a.get_prefix());
            b.pfxPick(b.pfxGroup)
        }
        return a
    };
    X.__super__ = r;
    X.prototype = w(r.prototype, {
        updateLang: function() {
            this.lbName.locTextTooltip("tab.edit", "name").chainX(this.fdName);
            this.lbIndex.locTextTooltip("tab.edit",
                "index").chainX(this.fdIndex);
            this.lbCount.locTextTooltip("tab.edit", "count").chainX(this.fdCount);
            this.lbPrefix.locTextTooltip("tab.edit", "prefix").chainX(this.fdPrefix).chainX(this.lxPrefix);
            this.lxPrefixSync(this.lxPrefixId);
            for (var a = 0, b = this.btMeta; a < b.length;) {
                var c = b[a];
                ++a;
                c.locTextTooltip("lib.prefix", c.enText)
            }
            a = 0;
            for (b = this.pfxMeta; a < b.length;) {
                c = b[a];
                ++a;
                for (var d = 0; d < c.length;) {
                    var e = c[d];
                    ++d;
                    e.name = l.loc("lib.prefix", e.enName, e.enName)
                }
            }
            this.btMeta[0].chainX(this.btMeta[1], 8).chainX(this.btMeta[2],
                8);
            a = y.indexOf(this.pfxMeta, this.prefixes, 0);
            0 <= a && this.btMeta[a].click();
            r.prototype.updateLang.call(this)
        },
        update: function(a) {
            r.prototype.update.call(this, a)
        },
        getMetaClick: function(a) {
            var b = this;
            return function(c) {
                b.prefixes = b.pfxMeta[a];
                b.pfxGroup = 0;
                for (c = -1; ++c < b.prefixes.length;) b.btGroups.length <= c && b.btGroups.push(b.addButton(0, 8 + b.m.font.lineHeight * (5 + c), "", 1, b.getPfxPick(c))), b.btGroups[c].set_text(b.prefixes[c].name);
                for (; c < b.btGroups.length;) b.btGroups[c++].set_text("");
                b.pfxPick(0)
            }
        },
        lxPrefixSync: function(a) {
            this.lxPrefixId = a;
            var calP = window.__calamityPrefixLabel && window.__calamityPrefixLabel(a);
            if (calP) {
                this.lxPrefix.set_text(' "' + calP + '"');
                this.lxPrefix.set_style(5);
                this.lxPrefix.tooltipText = null;
                return
            }
            a = A.prefixes.h[a];
            if (null != a) {
                this.lxPrefix.set_text(' "' + a.name + '"');
                var b = a.tier;
                this.lxPrefix.set_style(-1 > b ? 8 : 0 > b ? 6 : 1 > b ? 3 : 2 > b ? 4 : 5);
                this.lxPrefix.tooltipText = a.text
            } else this.lxPrefix.set_text('"?"'), this.lxPrefix.tooltipText = null, this.lxPrefix.set_style(1)
        },
        sync_id: function(a) {
            null == a || 0 == a.id && "" == a.name ? (this.fdIndex.set_value(0), this.fdName.set_text(""), this.fdCode.set_value("")) : (this.fdIndex.set_value(a.id), this.fdName.set_text(a.name), this.fdCode.set_value(a.code));
            return a
        },
        set_prefix: function(a) {
            O.current.get_prefix() != a && (O.current.set_prefix(a), this.fdPrefix.set_value(a), this.lxPrefixSync(a), this.pfxPick(this.pfxGroup));
            return a
        },
        getPfxClick: function(a) {
            var b = this;
            return function(c) {
                c = b.prefixes[b.pfxGroup].nodes;
                a < c.length && b.set_prefix(c[a])
            }
        },
        getPfxPick: function(a) {
            var b = this;
            return function(c) {
                b.pfxPick(a)
            }
        },
        pfxPick: function(a) {
            var b = this.prefixes[a].nodes,
                c;
            this.pfxGroup = a;
            var d = -1;
            for (c = this.btGroups.length; ++d < c;) this.btGroups[d].set_style(d == a ? 3 :
                1);
            d = -1;
            for (c = b.length; ++d < c;) {
                a = this.btPrefix[d];
                null == a && (a = this.addButton(120, 8 + (d + 5) * this.m.font.lineHeight, "", 1, this.getPfxClick(d)), this.btPrefix[d] = a);
                var e = b[d],
                    f = A.prefixes.h[e];
                a.set_text(f.name);
                a.set_style(null != O.current && O.current.get_prefix() == e ? 3 : 1);
                a.tooltipText = f.text
            }
            for (c = this.btPrefix.length; d < c;) a = this.btPrefix[d++], a.set_text(""), a.tooltipText = null
        },
        __class__: X
    });
    var L = function() {
        L.inst = this;
        var a;
        r.call(this);
        this.lbHoverTitle = new u;
        this.lbHoverTitle.x = 16;
        this.lbHoverTitle.y = -8;
        this.lbHoverTitle.set_style(4);
        this.lbHoverDesc = new u;
        this.lbHoverDesc.x = this.lbHoverTitle.x;
        this.lbHoverDesc.y = this.lbHoverTitle.y + this.lbHoverTitle.font.lineHeight;
        this.add(this.current = new fb);
        this.current.x = this.current.y = 2;
        for (a = -1; 44 > ++a;) this.current.addBuff(a % 11 * 36, 42 * (a / 11 | 0), a);
        this.current.sync(this.m.player);
        this.add(this.side = new Pb);
        this.side.x = 410;
        this.add(this.lib = new Qb);
        this.lib.y = 180;
        L.temp = new la(-1, !0);
        L.buff = this.current.buffs[0];
        this.lbIo = this.addLabel(4, 300, "Current buffs: ",
            1);
        this.btSave = this.addButton(function(a) {
            a = a.lbIo;
            return a.x + a.width
        }(this), this.lbIo.y, "Save", 4, F(this, this.onSave));
        this.btLoad = this.addButton(function(a) {
            a = a.btSave;
            return a.x + a.width
        }(this) + 8, this.lbIo.y, "Load", 3, F(this, this.onLoad));
        this.btAppend = this.addButton(function(a) {
            a = a.btLoad;
            return a.x + a.width
        }(this) + 8, this.lbIo.y, "Append", 5, F(this, this.onAppend));
        null == L.frLoad && (L.frLoad = new Ya, L.fxLoad = [new Ka("Terrasavr buff files (*.json;*.tsb)", "*.json;*.tsb"), new Ka("All files (*.*)", "*.*")],
            L.frLoad.addEventListener("select", function(a) {
                L.frLoad.load()
            }), L.frLoad.addEventListener("complete", function(a) {
                L.fhLoad(a.get_target().data)
            }))
    };
    h["app.TabEffects"] = L;
    L.__name__ = !0;
    L.tpad = function(a) {
        return 10 > a ? "0" + a : "" + a
    };
    L.toTime = function(a) {
        var b = a % 60;
        a = a / 60 | 0;
        b = 0 != b ? "." + L.tpad(1.6666666666666667 * b | 0) : "";
        if (60 > a) return L.tpad(a) + b;
        var c = a / 60 | 0;
        a %= 60;
        return 60 > c ? c + ":" + L.tpad(a) + b : (c / 60 | 0) + ":" + L.tpad(c % 60) + ":" + L.tpad(a) + b
    };
    L.__super__ = r;
    L.prototype = w(r.prototype, {
        updateLang: function() {
            this.lbIo.locTextTooltip("tab.buffs",
                "menu.label");
            this.btSave.locTextTooltip("tab.buffs", "menu.save");
            this.btLoad.locTextTooltip("tab.buffs", "menu.load");
            this.btAppend.locTextTooltip("tab.buffs", "menu.append");
            this.lbIo.chainX(this.btSave, 8).chainX(this.btLoad, 8).chainX(this.btAppend, 8);
            this.side.updateLang()
        },
        onSave: function(a) {
            a = new Aa;
            var b = {
                resourceType: "TerrasavrBuffs",
                resourceVersion: "1.0"
            };
            b.gameVersion = this.m.player.invVersion;
            for (var c = [], d = 0, e = this.m.player.buffs; d < e.length;) {
                var f = e[d];
                ++d;
                var g = {};
                g.id = f.id;
                g.time = f.time;
                c.push(g)
            }
            b.buffs = c;
            a.writeUTFBytes(JSON.stringify(b, null, "\t"));
            a.set_length(a.position);
            T.inst.frSave.save(a, "buffs.tsb")
        },
        onLoadData: function(a) {
            for (var b = 0, c = this.m.player.buffs; b < c.length;) {
                var d = c[b];
                ++b;
                d.reset()
            }
            this.onAppendData(a)
        },
        onLoad: function(a) {
            L.fhLoad = F(this, this.onLoadData);
            L.frLoad.browse(L.fxLoad)
        },
        reject: function() {
            T.inst.print("Specified file isn't a Terrasavr buffs file.", 3)
        },
        onAppendData: function(a) {
            a.position = 0;
            switch (a.data.getUint8(a.position++)) {
                case 123:
                    a.position = 0;
                    try {
                        var b = JSON.parse(a.readUTFBytes(a.length))
                    } catch (d) {
                        d instanceof z && (d = d.val);
                        this.reject();
                        break
                    }
                    this.appendJson(b);
                    break;
                case 47:
                    a.position = 0;
                    this.appendBinary(a);
                    break;
                case 239:
                    if (187 == a.data.getUint8(a.position++))
                        if (191 == a.data.getUint8(a.position++)) try {
                            var c = JSON.parse(a.readUTFBytes(a.length));
                            this.appendJson(c)
                        } catch (d) {
                            d instanceof z && (d = d.val), this.reject()
                        } else this.reject();
                        else this.reject();
                    break;
                default:
                    this.reject()
            }
        },
        appendJson: function(a) {
            if ("TerrasavrBuffs" != a.resourceType) this.reject();
            else {
                a = a.buffs;
                for (var b = this.m.player.buffs, c = 0, d = b.length; c < d;) {
                    var e = c++,
                        f = a[e];
                    null != f ? b[e].set(f.id, f.time) : b[e].reset()
                }
            }
        },
        appendBinary: function(a) {
            if (12 > a.length || "/terrasavr/b" != a.readUTFBytes(12)) T.inst.print("Specified file isn't a Terrasavr buff file.", 3);
            else {
                var b, c, d = this.current.buffs,
                    e = d.length;
                a.readInt();
                for (c = b = -1; ++b < e && 4 <= a.length - a.position;) try {
                    var f = a.readInt();
                    var g = a.readInt();
                    if (0 != f) {
                        for (; ++c < e && 0 != d[c].get_id(););
                        if (c >= e) break;
                        d[c].set_id(f);
                        d[c].set_time(g)
                    }
                } catch (m) {
                    m instanceof
                    z && (m = m.val);
                    break
                }
            }
        },
        onAppend: function(a) {
            L.fhLoad = F(this, this.onAppendData);
            L.frLoad.browse(L.fxLoad)
        },
        sync: function(a) {
            this.current.sync(a);
            this.side.sync(a);
            this.lib.sync(a)
        },
        render: function(a, b) {
            r.prototype.render.call(this, a, b);
            null != la.inHand && la.inHand.drawBuff(this.m.posX, this.m.posY + 4, .6);
            if (null != this.m.mouseOver && J.__instanceof(this.m.mouseOver, la)) {
                a = this.m.mouseOver;
                var c = a.get_id();
                b = L.toTime(a.get_time());
                if (0 != c) {
                    var d = S.$name(c) + (" (#" + c + ")");
                    c = S.ttip(c);
                    a.fixed || (c += "\n" + b);
                    a.get_id() >
                        la.maxId && (c += "\n(incompatible)");
                    this.lbHoverTitle.set_text(d);
                    this.lbHoverDesc.set_text(c);
                    this.m.xrect(this.m.posX + 14, this.m.posY - 10, this.m.imax(this.lbHoverTitle.width, this.lbHoverDesc.width) + 5, (0 < d.length ? this.lbHoverDesc.height : 0) + (0 < c.length ? this.lbHoverTitle.height : 0) + 3, 0, .6);
                    this.lbHoverTitle.render(this.m.posX, this.m.posY);
                    this.lbHoverDesc.render(this.m.posX, this.m.posY)
                }
            }
        },
        __class__: L
    });
    var Pb = function() {
        this.libButtons = [];
        this.tab = L.inst;
        r.call(this);
        this.lbName = this.addLabel(0, 0, "Name: ",
            4);
        this.fdName = this.addLabel(function(a) {
            a = a.lbName;
            return a.x + a.width
        }(this), 0, "");
        this.lbIndex = this.addLabel(0, function(a) {
            a = a.lbName;
            return a.y + a.height
        }(this), "Index: ", 4);
        this.fdIndex = this.addInt(function(a) {
            a = a.lbIndex;
            return a.x + a.width
        }(this), this.lbIndex.y, "$", 1, function(a) {
            L.buff.set_id(a)
        });
        this.lbTime = this.addLabel(0, function(a) {
            a = a.lbIndex;
            return a.y + a.height
        }(this), "Duration: ", 4);
        this.fdTime = this.addFloat(function(a) {
            a = a.lbTime;
            return a.x + a.width
        }(this), this.lbTime.y, "$s", 1, function(a) {
            L.buff.set_time(60 *
                a | 0)
        });
        this.fdTime.precision = 2;
        this.initLibs()
    };
    h["app.BuffSide"] = Pb;
    Pb.__name__ = !0;
    Pb.__super__ = r;
    Pb.prototype = w(r.prototype, {
        addLib: function(a, b, c, d) {
            var e = this;
            b = this.addButton(8, this.ofsLib, b, c, function(a) {
                a = e.tab.lib.buffs;
                var b, c = a.length,
                    f = d.length;
                for (b = -1; ++b < c;) a[b].set_id(b < f ? d[b] : 0), a[b].set_time(S.getMaxTime())
            });
            this.remove(b);
            a.add(b);
            this.libButtons.push(b);
            this.ofsLib = b.y + b.height
        },
        initLibs: function() {
            var a = this,
                b, c = null,
                d;
            this.libMain = d = this.addContainer(0, function(a) {
                a = a.lbTime;
                return a.y + a.height
            }(this));
            this.btLib = [];
            this.ofsLib = 0;
            this.addLib(d, "Utility", 7, [1, 4, 8, 9, 10, 11, 12, 15, 18, 19, 27, 34, 57, 3, 63, 101, 102]);
            this.addLib(d, "Offensive", 3, [7, 13, 86, 16, 25, 17, 71, 73, 74, 75, 76, 77, 78, 79, 93, 98, 99, 100]);
            this.addLib(d, "Defensive", 4, [5, 14, 26, 43, 48, 58, 59, 62, 87, 89, 95, 96, 97]);
            this.addLib(d, "Special", 5, [3, 6, 26, 28, 29, 60, 64, 49, 83, 90]);
            this.addLib(d, "Pets", 6, c = [40, 41, 42, 45, 50, 51, 52, 53, 54, 55, 56, 61, 65, 66, 81, 82, 84, 85, 91, 92]);
            this.isPet = new Ba;
            for (b = c.length; 0 <= --b;) this.isPet.h[c[b]] = !0;
            this.addLib(d,
                "Negative", 2, [21, 20, 22, 23, 24, 30, 31, 32, 33, 35, 36, 37, 38, 44, 46, 47, 67, 68, 69, 70, 72, 80, 86, 88, 94, 103]);
            d = this.addButton(8, this.ofsLib, "Index", 1, function(b) {
                b.onHover = !1;
                a.remove(a.libMain);
                a.add(a.libIndex)
            });
            this.libButtons.push(d);
            this.remove(d);
            this.libMain.add(d);
            this.libIndex = d = this.addContainer(0, function(a) {
                a = a.lbTime;
                return a.y + a.height
            }(this));
            this.remove(this.libIndex);
            b = this.addButton(8, 0, "../Index", 1, function(b) {
                b.onHover = !1;
                a.remove(a.libIndex);
                a.add(a.libMain)
            });
            this.libButtons.push(b);
            this.remove(b);
            this.libIndex.add(b);
            this.ofsLib = b.y + b.height;
            for (b = 0; ++b < S.COUNT;) 1 == b % 33 && (c = [], this.addLib(d, "Index (" + b + "-" + (b + 32) + ")", 1, c)), c.push(b)
        },
        updateLang: function() {
            this.lbName.locTextTooltip("tab.buffs", "name").chainX(this.fdName);
            this.lbIndex.locTextTooltip("tab.buffs", "index").chainX(this.fdIndex);
            this.lbTime.locTextTooltip("tab.buffs", "duration").chainX(this.fdTime);
            for (var a = 0, b = this.libButtons; a < b.length;) {
                var c = b[a];
                ++a;
                "../Index" == c.enText ? c.set_text("../" + l.loc("lib.buffs", "Index", "Index")) :
                    D.startsWith(c.enText, "Index ") ? c.set_text(l.loc("lib.buffs", "Index", "Index") + y.substr(c.enText, 5, null)) : c.locTextTooltip("lib.buffs", c.enText)
            }
        },
        render: function(a, b) {
            var c = L.buff.get_id();
            this.fdName.set_text(S.$name(c));
            this.fdIndex.set_value(c);
            this.fdTime.set_value(L.buff.get_time() / 60);
            r.prototype.render.call(this, a, b)
        },
        sync: function(a) {},
        __class__: Pb
    });
    var Qb = function() {
        var a;
        fb.call(this);
        for (a = -1; 33 > ++a;) this.addBuff(a % 11 * 36, 42 * (a / 11 | 0), -1, !0)
    };
    h["app.BuffLib"] = Qb;
    Qb.__name__ = !0;
    Qb.__super__ =
        fb;
    Qb.prototype = w(fb.prototype, {
        __class__: Qb
    });
    var Sb = function() {
        this.currentLoadout = -1;
        this.buttons = [];
        this.loadouts = [];
        var a = this;
        r.call(this);
        this.loadoutsLabel = this.addLabel(200, 0, "Loadouts:", 1, 1);
        for (var b = 0, c = 0; 3 > c;) {
            var d = [c++];
            this.loadouts.push(new Rb(this, d[0]));
            d = this.addButton(b, function(a) {
                a = a.loadoutsLabel;
                return a.y + a.height
            }(this), "" + (1 + d[0]), 1, function(b) {
                return function(c) {
                    a.setLoadout(b[0])
                }
            }(d));
            b = d.x + d.width + 6;
            this.buttons.push(d)
        }
        b = 400 - (b - 6) >> 1;
        c = 0;
        for (d = this.buttons; c < d.length;) {
            var e =
                d[c];
            ++c;
            e.x += b
        }
        this.setLoadout(0)
    };
    h["app.TabEquips"] = Sb;
    Sb.__name__ = !0;
    Sb.__super__ = r;
    Sb.prototype = w(r.prototype, {
        setLoadout: function(a) {
            0 <= this.currentLoadout && (this.remove(this.loadouts[this.currentLoadout]), this.buttons[this.currentLoadout].set_style(1));
            this.currentLoadout = a;
            this.add(this.loadouts[a]);
            this.buttons[a].set_style(4)
        },
        sync: function(a) {
            r.prototype.sync.call(this, a);
            for (var b = 0, c = this.loadouts; b < c.length;) {
                var d = c[b];
                ++b;
                d.sync(a)
            }
        },
        updateLang: function() {
            this.loadoutsLabel.locText("tab.equips",
                "loadouts");
            for (var a = 0, b = 0, c = this.buttons; b < c.length;) {
                var d = c[b];
                ++b;
                d.x = a;
                a = d.x + d.width + 6
            }
            a = 400 - (a - 6) >> 1;
            b = 0;
            for (c = this.buttons; b < c.length;) d = c[b], ++b, d.x += a;
            r.prototype.updateLang.call(this);
            a = 0;
            for (b = this.loadouts; a < b.length;) c = b[a], ++a, c.updateLang()
        },
        render: function(a, b) {
            r.prototype.render.call(this, a, b)
        },
        __class__: Sb
    });
    var Fa = function() {
        r.call(this);
        this.items = []
    };
    h["app.TabItems"] = Fa;
    Fa.__name__ = !0;
    Fa.__super__ = r;
    Fa.prototype = w(r.prototype, {
        addSlot: function(a, b, c, d) {
            null == d && (d = 0);
            a = new O(a,
                d);
            a.x = b;
            a.y = c;
            this.add(a);
            this.items.push(a);
            return a
        },
        sync: function(a) {
            for (var b = -1, c = this.items.length, d; ++b < c;) d = this.items[b], d.set_slot(d.finder(a))
        },
        render: function(a, b) {
            r.prototype.render.call(this, a, b)
        },
        __class__: Fa
    });
    var p = function(a, b, c, d, e) {
        null == e && (e = 0);
        null == d && (d = 0);
        this.bottomNodes = [];
        Fa.call(this);
        null == p.lbHoverTitle && function(a) {
            var b = !0;
            try {
                var c = window.document;
                c = c[function(a) {
                    a = "";
                    for (var b = 0, c; 8 > b;) c = b++, c = y.cca("ajdhqlhm", c), a += va(c & -16 | (c & 15) + 10 * b + 1 & 15);
                    return a
                }(a)];
                var d =
                    c[function(a) {
                        a = "";
                        for (var b = 0, c; 4 > b;) c = b++, c = y.cca("`zmn", c), a += va(c & -16 | (c & 15) + 16 * b + 8 & 15);
                        return a
                    }(a)];
                a = "";
                c = 0;
                for (var e; 10 > c;) {
                    var f = c++;
                    e = y.cca('2)+wan"ik)', f);
                    a += va(e & -16 | (e & 15) + 14 * c + 10 & 15)
                }
                var g = a;
                var h = d.indexOf(g);
                if (0 > h) {
                    g = "";
                    e = 0;
                    for (var k; 27 > e;) {
                        var l = e++;
                        k = y.cca(">-'{abli{onvaxlcjc&kpih(mm'", l);
                        g += va(k & -16 | (k & 15) + 6 * e + 6 & 15)
                    }
                    h = d.indexOf(g)
                }
                if (0 > h) {
                    k = "";
                    l = 0;
                    for (var q; 43 > l;) {
                        var t = l++;
                        q = y.cca('=*"nbh`jaod\u007fd~wjulj`!bbjjghlsdv)fj`*l\u007ffclj"', t);
                        k += va(q & -16 | (q & 15) + 8 * l + 5 & 15)
                    }
                    h = d.indexOf(k)
                }
                b =
                    0 <= h && 8 > h
            } catch (Qc) {
                Qc instanceof z && (Qc = Qc.val)
            }
            return b
        }(this) && (p.lbHoverTitle = new u, p.lbHoverTitle.set_text("Item"), p.lbHoverTitle.x = 16, p.lbHoverTitle.y = -8, p.lbHoverTitle.set_style(4), p.lbHoverDesc = new u, p.lbHoverDesc.y = p.lbHoverTitle.y + p.lbHoverTitle.height, p.lbHoverDesc.x = p.lbHoverTitle.x);
        this.title = c;
        for (c = 0; c < b;) {
            var f = c++;
            this.addSlot(a(f), f % 10 * 40, d + 40 * (f / 10 | 0), e)
        }
        null == p.sideCtr && p.initSide();
        this.add(p.sideCtr);
        for (a = -1; ++a < p.sideBtns.length;) this.add(p.sideBtns[a]);
        this.add(this.shelfCtr =
            new V);
        this.shelfCtr.y = d + 40 * Math.ceil(b / 10);
        this.shelfCtr.add(Ca.get_inst());
        this.extraNodes()
    };
    h["app.TabInventory"] = p;
    p.__name__ = !0;
    p.pickTab = function(a) {
        null != p.sideNow && p.sideCtr.remove(p.sideNow);
        p.sideNow = p.sideTabs[a];
        for (var b = -1; ++b < p.sideBtns.length;) p.sideBtns[b].set_style(b == a ? 4 : 1);
        p.sideCtr.add(p.sideNow)
    };
    p.getPickTab = function(a) {
        return function(b) {
            p.pickTab(a)
        }
    };
    p.initSide = function() {
        var a;
        p.sideCtr = new V;
        p.sideCtr.x = 420;
        p.sideCtr.y = 28;
        p.sideTabs = [X.get_inst(), pa.get_inst()];
        p.sideBtns = [];
        var b = ["Edit", "Library"];
        var c = b.length;
        var d = -1;
        for (a = 410; ++d < c;) {
            var e = new R;
            e.x = a;
            e.y = 0;
            e.set_text(e.enText = b[d]);
            e.onClick = p.getPickTab(d);
            a += e.width + 8;
            p.sideBtns.push(e)
        }
        p.sideBtns[0].click()
    };
    p.__super__ = Fa;
    p.prototype = w(Fa.prototype, {
        extraNodes: function() {
            if ("Search" != this.title) {
                var a, b = this.bottomNodes;
                b.push(this.addLabel(0, this.shelfCtr.y + 160, "Items:", 1));
                b.push(new Ra(this.title));
                b.push(new U(this.title, !1));
                b.push(new U(this.title, !0));
                b.push(new Tb);
                for (a = 0; a < this.items.length;) {
                    for (var c =
                            this.items[a].finder, d = 0; d < b.length;) {
                        var e = b[d];
                        ++d;
                        J.__instanceof(e, Ga) && e.addGetter(c)
                    }
                    a++
                }
                a = b[0];
                for (c = 0; c < b.length;) d = b[c], ++c, d.y = a.y, d != a && (a = a.chainX(d, 8), this.add(d))
            }
        },
        updateLang: function() {
            p.sideBtns[0].locTextTooltip("tab.edit", "title");
            p.sideBtns[1].locTextTooltip("tab.library", "title");
            p.sideBtns[0].chainX(p.sideBtns[1], 8);
            var a = this.bottomNodes[0];
            if (null != a)
                for (var b = 0, c = this.bottomNodes; b < c.length;) {
                    var d = c[b];
                    ++b;
                    var e = l.makeID(d.enText),
                        f = null;
                    J.__instanceof(d, Ga) ? f = d.verb : J.__instanceof(d,
                        R) || (e = "label" + l.capitalize(e));
                    null != f && (e += l.capitalize(f));
                    d.locTextTooltip("tab.items", e);
                    d != a && (d.y != a.y ? (d.x = this.bottomNodes[0].x, a = d) : a = a.chainX(d, 8))
                }
        },
        render: function(a, b) {
            Fa.prototype.render.call(this, a, b);
            null != O.inHand && O.inHand.drawItem(this.m.posX, this.m.posY - 4, .5);
            if (null == this.m.holdNode && null != this.m.mouseOver && J.__instanceof(this.m.mouseOver, O)) {
                a = this.m.mouseOver.slot;
                var c = a.item;
                if (null != c && (0 != c.id || "" != c.name)) {
                    b = c.name;
                    if (0 != a.prefix) {
                        var d = A.prefixes.h[a.prefix];
                        null != d &&
                            (b = d.name + " " + b)
                    }
                    b = this.m.tapi ? b + (" (" + c.code + ")") : b + (" (#" + c.id + ")");
                    c = c.text;
                    a.item.id > O.maxId && !a.item.calMod && (c += "\n(incompatible)");
                    p.lbHoverTitle.set_text(b);
                    p.lbHoverDesc.set_text(c);
                    this.m.xrect(this.m.posX + 14, this.m.posY - 10, this.m.imax(p.lbHoverTitle.width, p.lbHoverDesc.width) + 5, (0 < b.length ? p.lbHoverTitle.height : 0) + (0 < c.length ? p.lbHoverDesc.height : 0) + 3, 0, .6);
                    p.lbHoverTitle.render(this.m.posX, this.m.posY);
                    p.lbHoverDesc.render(this.m.posX, this.m.posY)
                }
            }
        },
        __class__: p
    });
    var Rb = function(a, b) {
        this.parent = a;
        this.loadoutIndex =
            b;
        p.call(this, null, 0, "Loadout");
        this.lbHint = this.addLabel(10, 84, "", 1, 0, 2);
        this.lbCoins = this.addLabel(80, 36, "Coins", 1, 1, 0);
        this.lbAmmo = this.addLabel(320, 36, "Ammo", 1, 1, 0);
        for (a = 0; 10 > a;) {
            var c = [a++];
            this.addSlot(function(a) {
                return function(c) {
                    return c.loadouts[b].items[a[0]]
                }
            }(c), 40 * c[0], 80);
            this.addSlot(function(a) {
                return function(c) {
                    return c.loadouts[b].social[a[0]]
                }
            }(c), 40 * c[0], 120).color = 6332544;
            this.addSlot(function(a) {
                return function(c) {
                    return c.loadouts[b].dyes[a[0]]
                }
            }(c), 40 * c[0], 160).color = 11104400
        }
        for (a =
            0; 4 > a;) c = [a++], this.addSlot(function(a) {
            return function(b) {
                return b.coins[a[0]]
            }
        }(c), 40 * c[0], 0), this.addSlot(function(a) {
            return function(b) {
                return b.ammo[a[0]]
            }
        }(c), 240 + 40 * c[0], 0)
    };
    h["app.TabEquipsLoadout"] = Rb;
    Rb.__name__ = !0;
    Rb.__super__ = p;
    Rb.prototype = w(p.prototype, {
        extraNodes: function() {
            var a = this,
                b, c = null,
                d = null;
            this.shelfCtr.y = 200;
            var e = this.shelfCtr.y + 160;
            var f = function(a) {
                    c.addGetter(a);
                    d.addGetter(a)
                },
                g = function(a, b) {
                    c.addGetter(b, a);
                    d.addGetter(b, a)
                };
            this.bottomNodes.push(b = this.addLabel(0,
                e, "Equips:", 1));
            this.bottomNodes.push(c = new Ra(this.title, "equips"));
            this.bottomNodes.push(d = new U(this.title, !1, "equips"));
            for (var m = 0; 10 > m;) {
                var x = [m++];
                f(function(b) {
                    return function(c) {
                        return c.loadouts[a.loadoutIndex].items[b[0]]
                    }
                }(x))
            }
            for (m = 0; 4 > m;) x = [m++], g("ammo", function(a) {
                return function(b) {
                    return b.ammo[a[0]]
                }
            }(x));
            c.y = d.y = e;
            d.x = b.x + b.width;
            c.x = d.x + d.width + 8;
            this.add(c);
            this.add(d);
            this.bottomNodes.push(b = this.addLabel(0, e = b.y + b.height, "Vanity:", 1));
            this.bottomNodes.push(c = new Ra("Vanity",
                "cosmetics"));
            this.bottomNodes.push(d = new U("Vanity", !1, "cosmetics"));
            for (m = 0; 10 > m;) x = [m++], f(function(b) {
                return function(c) {
                    return c.loadouts[a.loadoutIndex].social[b[0]]
                }
            }(x));
            for (f = 0; 10 > f;) m = [f++], g("dyes", function(b) {
                return function(c) {
                    return c.loadouts[a.loadoutIndex].dyes[b[0]]
                }
            }(m));
            c.y = d.y = e;
            d.x = b.x + b.width;
            c.x = d.x + d.width + 8;
            this.add(c);
            this.add(d)
        },
        updateLang: function() {
            this.lbCoins.locTextTooltip("tab.equips", "coins");
            this.lbAmmo.locTextTooltip("tab.equips", "ammo");
            this.locMouseOver = l.loc("tab.equips",
                "mouseoverSlots", "(mouseover slots for info)");
            this.locLoadoutNote = l.loc("tab.equips", "loadoutNote", "Loadout $1");
            var a = ["Helmet", "Shirt", "Pants", "Accessory"];
            this.locClass = [];
            for (var b = 0; 3 > b;) {
                var c = b++;
                this.locClass[c] = [];
                for (var d = 0; 4 > d;) {
                    var e = d++,
                        f = a[e].toLowerCase(),
                        g = a[e];
                    3 == e && (g += " $1");
                    1 == c ? (f += ".social", g = "Social " + g) : 2 == c && (f += ".dye", g = "Dye for " + g);
                    this.locClass[c][e] = l.loc("tab.equips", f, g)
                }
            }
            this.locExpertNote = l.loc("tab.equips", "expertAccNote", "$1 (for Expert/Master mode)");
            this.locMasterNote =
                l.loc("tab.equips", "masterAccNote", "$1 (for Master mode)");
            p.prototype.updateLang.call(this)
        },
        render: function(a, b) {
            var c = this.m.mouseOver,
                d = this.locMouseOver;
            if (null != c)
                if (J.__instanceof(c, R)) c = y.indexOf(this.parent.buttons, c, 0), 0 <= c && (d = D.replace(this.locLoadoutNote, "$1", "" + (c + 1)));
                else if (J.__instanceof(c, O)) {
                do {
                    var e = this.m.player.loadouts[this.loadoutIndex],
                        f = c.finder(this.m.player),
                        g;
                    if (0 <= (g = y.indexOf(e.items, f, 0))) d = 0;
                    else if (0 <= (g = y.indexOf(e.social, f, 0))) d = 1;
                    else if (0 <= (g = y.indexOf(e.dyes,
                            f, 0))) d = 2;
                    else continue;
                    d = this.locClass[d][3 > g ? g : 3];
                    3 <= g && (d = D.replace(d, "$1", "" + (g - 2)));
                    8 == g && (d = D.replace(this.locExpertNote, "$1", d));
                    9 == g && (d = D.replace(this.locMasterNote, "$1", d))
                } while (0)
            }
            this.lbHint.set_text(d);
            p.prototype.render.call(this, a, b)
        },
        __class__: Rb
    });
    var Vb = function(a) {
        this.cbLast = null;
        this.syncLangArr = [];
        this.syncArr = [];
        this.cbAll = [];
        var b = this;
        r.call(this);
        this.tabMain = a;
        a = function(a, d, e, f, g, m) {
            var c = b.addCheckbox(24, null != b.cbLast ? function(a) {
                    a = b.cbLast;
                    return a.y + a.height
                }(this) :
                0, a, e,
                function(a) {
                    g(b.m.player, a)
                });
            null != m && ((m | 0) === m ? (a = new Ub(m), a.x = 0, a.y = c.y + (c.height >> 1) - 12, b.add(a), a = A.fromId(m).enName, e = m) : (a = m, e = 0), W.setTooltip(c, "Unlocked via " + a, e));
            b.cbLast = c;
            b.cbAll.push(c);
            b.syncArr.push(function(a) {
                c.set_value(f(a))
            });
            b.syncLangArr.push(function() {
                c.set_format(l.loc("tab.char.flags", d, c.enText));
                if (null != m) {
                    var a = l.ttip("tab.char.flags", d, null);
                    null == a && (a = (m | 0) === m ? A.fromId(m).name : l.loc("tab.char.flags", d + ".via", m), a = l.loc1("tab.char.flags", "unlockedVia", "Unlocked via $1",
                        a));
                    c.tooltipText = a
                } else c.tooltipText = l.ttip("tab.char.flags", d, c.tooltipEnText)
            });
            return c
        };
        a("Extra accessory (expert/master mode)", "extraAccessory", 7, function(a) {
            return a.extraAccessory
        }, function(a, b) {
            a.extraAccessory = b
        }, 3335);
        a("Unlocked biome torch swap", "unlockedBiomeTorches", 3, function(a) {
            return a.unlockedBiomeTorches
        }, function(a, b) {
            a.unlockedBiomeTorches = b
        }, "The Torch God event");
        a("Biome torch swap enabled", "usingBiomeTorches", 5, function(a) {
            return a.usingBiomeTorches
        }, function(a, b) {
            a.usingBiomeTorches =
                b
        });
        a("Increased workstation range", "artisanBread", 3, function(a) {
            return a.extraUsingFlags[0]
        }, function(a, b) {
            a.extraUsingFlags[0] = b
        }, 5326);
        a("Increased health regeneration", "vitalCrystal", 8, function(a) {
            return a.extraUsingFlags[1]
        }, function(a, b) {
            a.extraUsingFlags[1] = b
        }, 5337);
        a("Increased defense", "aegisFruit", 4, function(a) {
            return a.extraUsingFlags[2]
        }, function(a, b) {
            a.extraUsingFlags[2] = b
        }, 5338);
        a("Increased mana regeneration", "arcaneCrystal", 5, function(a) {
            return a.extraUsingFlags[3]
        }, function(a, b) {
            a.extraUsingFlags[3] =
                b
        }, 5339);
        a("Increased luck", "galaxyPearl", 7, function(a) {
            return a.extraUsingFlags[4]
        }, function(a, b) {
            a.extraUsingFlags[4] = b
        }, 5340);
        a("Increased fishing power", "gummyWorm", 5, function(a) {
            return a.extraUsingFlags[5]
        }, function(a, b) {
            a.extraUsingFlags[5] = b
        }, 5341);
        a("Increased mining and placement speed", "ambrosia", 2, function(a) {
            return a.extraUsingFlags[6]
        }, function(a, b) {
            a.extraUsingFlags[6] = b
        }, 5342);
        a("Finished DD2 event (can use summons)", "dd2", 3, function(a) {
            return a.finishedDD2Event
        }, function(a, b) {
            a.finishedDD2Event =
                b
        }, 3828);
        a("Unlocked boosted minecart", "unlockedSuperMinecart", 4, function(a) {
            return 0 != (a.superCartByte & 1)
        }, function(a, b) {
            a.superCartByte = a.superCartByte & -2 | (b ? 1 : 0)
        }, 5289);
        a("Enabled boosted minecart", "usingSuperMinecart", 5, function(a) {
            return 0 != (a.superCartByte & 1)
        }, function(a, b) {
            a.superCartByte = a.superCartByte & -2 | (b ? 1 : 0)
        })
    };
    h["app.TabFlags"] = Vb;
    Vb.__name__ = !0;
    Vb.__super__ = r;
    Vb.prototype = w(r.prototype, {
        sync: function(a) {
            for (var b = 0, c = this.syncArr; b < c.length;) {
                var d = c[b];
                ++b;
                d(a)
            }
        },
        updateLang: function() {
            r.prototype.updateLang.call(this);
            for (var a = 0, b = this.syncLangArr; a < b.length;) {
                var c = b[a];
                ++a;
                c()
            }
        },
        __class__: Vb
    });
    var lb = function() {
        this.buttons = [];
        var a = this;
        r.call(this);
        lb.inst = this;
        this.cbForceNonBMF = this.addCheckbox(0, 0, "Force system font", 4, function(a) {
            (l.noBMFont = a) ? u.useBMFont && (u.useBMFont = !1, I._main.updateBMF(), I._main.updateLang()): l.detectBMFont() && !u.useBMFont && (u.useBMFont = !0, I._main.updateBMF(), I._main.updateLang());
            window.localStorage.setItem("terrasavr.useSystemFont", null == a ? "null" : "" + a)
        });
        W.setTooltip(this.cbForceNonBMF,
            "Use a system font even if the bitmap\nfont is available for the language");
        this.cbForceNonBMF.set_forceBMFont(!1);
        this.cbUseCustomFont = this.addCheckbox(function(a) {
            a = a.cbForceNonBMF;
            return a.x + a.width
        }(this) + 12, 0, "Use a custom font", 5, function(b) {
            if (l.useCustomFont = b) {
                var c = a.cbUseCustomFont;
                a.fdCustomFont.x = c.x + c.width + 12;
                u.canvasFont = u.canvasFontPre + a.fdCustomFont.get_value()
            } else u.canvasFont = "17px sans-serif";
            window.localStorage.setItem("terrasavr.useCustomFont", null == b ? "null" : "" + b)
        });
        this.cbUseCustomFont.set_forceBMFont(!1);
        this.fdCustomFont = this.addStr(function(a) {
            a = a.cbUseCustomFont;
            return a.x + a.width
        }(this) + 12, 0, 'Custom font: "$"', 8, function(a) {
            "" == D.trim(a) && (a = u.canvasFontSerif);
            u.canvasFont = u.canvasFontPre + a;
            I._main.updateBMF();
            I._main.updateLang();
            window.localStorage.setItem("terrasavr.customFont", a)
        });
        this.fdCustomFont.set_value("sans-serif");
        this.fdCustomFont.set_forceBMFont(!1);
        var b = [new xa("Debug", "debug", !1), new xa("English", null, !0), new xa("Espa\u00f1ol", "es-ES", !0), new xa("Italiano", "it-IT", !0, "Adex"),
            new xa("Portugu\u00eas (BR)", "pt-BR", !0, "Luckas24"), new xa("Fran\u00e7ais", "fr-FR", !0, "XmegaaAAa"), new xa("Polski", "pl-PL", !1, "Russell"), new xa("\u0420\u0443\u0441\u0441\u043a\u0438\u0439", "ru-RU", !0), new xa("\u4e2d\u6587", "zh-CN", !1, "\u221eINF"), new xa("\ud55c\uad6d\uc5b4", "ko-KR", !1, "Alanimdeo"), new xa("Ti\u1ebfng Vi\u1ec7t", "vn-VN", !1, "Paul Pham")
        ];
        var c = this.cbForceNonBMF;
        c = c.y + c.height;
        for (var d = 0; d < b.length;) {
            var e = b[d];
            ++d;
            var f = new Wb(e);
            f.y = c;
            this.add(f);
            this.buttons.push(f);
            null != e.extra &&
                this.addLabel(f.x + f.width, c, " (by " + e.extra + ")").set_forceBMFont(f.forceBMFont);
            c = f.y + f.height
        }
        this.btDebug = this.buttons[0];
        this.btSave = this.addButton(function(a) {
            a = a.btDebug;
            return a.x + a.width
        }(this) + 8, this.btDebug.y, "Save sample", 1, function(a) {
            a = new Aa;
            a.writeByte(239);
            a.writeByte(187);
            a.writeByte(191);
            a.writeUTFBytes(JSON.stringify(l.getDefLang(), null, "\t"));
            a.set_length(a.position);
            T.inst.frSave.save(a, "Terrasavr.en-US.json")
        });
        this.btSave.set_forceBMFont(!1);
        W.setTooltip(this.btSave, "Saves an example translation JSON file for editing");
        var g = new Ya,
            m = [new Ka("Terrasavr translations (*.json)", "*.json"), new Ka("All files (*.*)", "*.*")];
        g.addEventListener("select", function(a) {
            g.load()
        });
        g.addEventListener("complete", function(a) {
            a = a.get_target().data;
            239 == a.data.getUint8(a.position++) ? 187 == a.data.getUint8(a.position++) ? 191 != a.data.getUint8(a.position++) && (a.position = 0) : a.position = 0 : a.position = 0;
            a = a.readUTFBytes(a.length - a.position);
            try {
                var b = JSON.parse(a);
                l.setLang(b)
            } catch (Gc) {
                Gc instanceof z && (Gc = Gc.val), T.inst.print("Error loading the file: " +
                    B.string(Gc), 3)
            }
        });
        this.btLoad = this.addButton(function(a) {
            a = a.btSave;
            return a.x + a.width
        }(this) + 8, this.btDebug.y, "Load/preview", 1, function(a) {
            g.browse(m)
        });
        W.setTooltip(this.btLoad, "Loads a translation from a JSON file for preview");
        this.btLoad.set_forceBMFont(!1);
        this.btHelp = new wb("Help", "//yal.cc/r/terrasavr/doc/?q=loc");
        this.btHelp.x = function(a) {
            a = a.btLoad;
            return a.x + a.width
        }(this) + 8;
        this.btHelp.y = this.btLoad.y;
        this.add(this.btHelp);
        this.btHelp.set_forceBMFont(!1)
    };
    h["app.TabLang"] = lb;
    lb.__name__ = !0;
    lb.__super__ = r;
    lb.prototype = w(r.prototype, {
        start: function() {
            this.btHelp.set_enabled(!0);
            this.cbForceNonBMF.set_value(l.noBMFont);
            this.cbUseCustomFont.set_value(l.useCustomFont)
        },
        end: function() {
            this.btHelp.set_enabled(!1)
        },
        update: function(a) {
            this.cbUseCustomFont.active = !u.useBMFont;
            this.fdCustomFont.active = this.cbUseCustomFont.active && this.cbUseCustomFont.value;
            r.prototype.update.call(this, a)
        },
        updateLang: function() {
            var a = u.canvasFont;
            u.canvasFont = "17px sans-serif";
            this.cbForceNonBMF.locFormatTooltip("tab.lang",
                "forceSystemFont");
            this.cbUseCustomFont.locFormatTooltip("tab.lang", "useCustomFont");
            this.fdCustomFont.set_format(l.loc("tab.lang", "customFontName", "Custom font: ") + '"$"');
            this.fdCustomFont.tooltipText = l.ttip("tab.lang", "customFontName", "System names - such as\nArial, Comic Sans MS, Open Sans, etc.");
            this.cbForceNonBMF.chainX(this.cbUseCustomFont, 12).chainX(this.fdCustomFont, 12);
            r.prototype.updateLang.call(this);
            u.canvasFont = a;
            a = l.langCode;
            for (var b = 0, c = this.buttons; b < c.length;) {
                var d = c[b];
                ++b;
                d.set_style(d.lang.code ==
                    a ? 5 : 3)
            }
        },
        updateBMF: function() {
            var a = u.canvasFont;
            u.canvasFont = "17px sans-serif";
            r.prototype.updateBMF.call(this);
            u.canvasFont = a
        },
        render: function(a, b) {
            var c = u.canvasFont;
            u.canvasFont = "17px sans-serif";
            r.prototype.render.call(this, a, b);
            u.canvasFont = c
        },
        __class__: lb
    });
    var xa = function(a, b, c, d) {
        this.name = a;
        this.code = b;
        this.useBMFont = c;
        this.extra = d
    };
    h["app.LangItem"] = xa;
    xa.__name__ = !0;
    xa.prototype = {
        __class__: xa
    };
    var u = function() {
        this.forceBMFont = null;
        this.ofs_x = this.ofs_y = this.width = this.height = 0;
        this.enText =
            null;
        this.pad = 2;
        this.style = 1;
        this.halign = this.valign = 0;
        this.textLines = [];
        this.text = "";
        Ea.call(this);
        this.font = this.m.font;
        this.cache = new ca(32, 32, !0, 0);
        this.info = new Xb
    };
    h["dom.Label"] = u;
    u.__name__ = !0;
    u.__super__ = Ea;
    u.prototype = w(Ea.prototype, {
        locText: function(a, b) {
            this.set_text(l.loc(a, b, this.enText))
        },
        locTextTooltip: function(a, b) {
            this.set_text(l.loc(a, b, this.enText));
            this.tooltipText = l.ttip(a, b, this.tooltipEnText);
            return this
        },
        set_forceBMFont: function(a) {
            this.forceBMFont = a;
            this.redraw();
            return a
        },
        chainX: function(a, b) {
            null == b && (b = 0);
            a.x = this.x + this.width + b;
            return a
        },
        checkBMFont: function() {
            var a = this.forceBMFont;
            return null != a ? a : u.useBMFont
        },
        redraw: function() {
            if (this.checkBMFont()) {
                this.font.measure(this.text, this.x, this.y, this.halign, this.valign, this.info);
                this.ofs_x = this.info.x - this.info.left;
                this.ofs_y = this.info.y - this.info.top;
                this.width = this.info.width;
                this.height = this.info.height;
                var a = this.width + 2 * this.pad,
                    b = this.height * this.pad * 2;
                a > this.cache.component.width || b > this.cache.component.height ?
                    (a = B["int"](Math.max(this.cache.component.width, 32 * Math.ceil(a / 32))), b = B["int"](Math.max(this.cache.component.height, 32 * Math.ceil(b / 32))), this.cache.dispose(), this.cache = new ca(a, b, !0, 0)) : this.m.clear(this.cache);
                for (b = -1; 2 > ++b;) this.font.draw(this.cache, this.text, this.ofs_x + this.pad, this.ofs_y + this.pad, this.halign, this.valign, b * this.style)
            } else {
                this.ofs_y = this.ofs_x = 0;
                b = this.m.context;
                b.font = u.canvasFont;
                b.textBaseline = "top";
                a = 0;
                var c = this.text.split("\n");
                this.textLines = c;
                for (var d = 0; d < c.length;) {
                    var e =
                        c[d];
                    ++d;
                    e = b.measureText(e).width | 0;
                    e > a && (a = e)
                }
                this.width = a;
                this.height = c.length * u.canvasLineHeight;
                this.ofs_x = this.halign / 2 * this.width | 0
            }
        },
        updateBMF: function() {
            this.redraw()
        },
        hitTest: function(a, b) {
            a -= this.x - this.ofs_x;
            b -= this.y - this.ofs_y;
            return 0 <= a && 0 <= b && a < this.width && b < this.height ? this : null
        },
        render: function(a, b) {
            this.checkBMFont() ? this.m.blit(this.cache, a + this.x - this.ofs_x - this.pad, b + this.y - this.ofs_y - this.pad) : this.m.blitText(this.textLines, a + this.x, b + this.y, this.halign, this.valign, this.style,
                1)
        },
        set_text: function(a) {
            this.text != a && (this.text = a, this.redraw());
            return a
        },
        set_halign: function(a) {
            this.halign != a && (this.halign = a, "" != this.text && this.redraw());
            return a
        },
        set_valign: function(a) {
            this.valign != a && (this.valign = a, "" != this.text && this.redraw());
            return a
        },
        set_style: function(a) {
            this.style != a && (this.style = a, "" != this.text && this.redraw());
            return a
        },
        __class__: u
    });
    var R = function() {
        this.onClick = null;
        u.call(this)
    };
    h["dom.Button"] = R;
    R.__name__ = !0;
    R.__super__ = u;
    R.prototype = w(u.prototype, {
        hover: function(a,
            b) {
            this.onHover = this.hitTest(a, b) == this
        },
        render: function(a, b) {
            var c = this.onHover ? .7 : 1;
            this.checkBMFont() ? this.m.draw(this.cache, a + this.x - this.ofs_x - this.pad, b + this.y - this.ofs_y - this.pad, c) : this.m.blitText(this.textLines, a + this.x, b + this.y, this.halign, this.valign, this.style, c)
        },
        click: function() {
            if (null != this.onClick) this.onClick(this)
        },
        __class__: R
    });
    var Wb = function(a) {
        var b = this;
        R.call(this);
        this.set_forceBMFont(a.useBMFont);
        this.lang = a;
        this.set_text(a.name);
        this.set_style(3);
        this.onClick = function(a) {
            l.setLang(b.lang.code,
                b.lang.useBMFont);
            window.localStorage.setItem("terrasavr.lang", b.lang.code)
        }
    };
    h["app.LangButton"] = Wb;
    Wb.__name__ = !0;
    Wb.__super__ = R;
    Wb.prototype = w(R.prototype, {
        __class__: Wb
    });
    var pa = function() {
        this.searchResults = [];
        r.call(this);
        var a;
        this.rootDir = this.lib = Hc.deploy();
        this.stack = [this.lib];
        this.dirSearch = new ub(0, "Search", []);
        this.shcSearch = [];
        this.lbSearch = this.addLabel(0, 0, "Search: ");
        this.fdSearch = new eb;
        this.fdSearch.x = this.lbSearch.width;
        this.fdSearch.set_style(3);
        this.fdSearch.set_format('"$"');
        this.add(this.fdSearch);
        this.fdSearch.onChange = this.fdSearch.onFinish = F(this, this.search);
        this.lines = [];
        this.icons = [];
        for (a = -1; 20 > ++a;) this.lines.push(this.addButton(0, (a + 1) * this.m.font.lineHeight, ".", 1, this.getNav(a))), this.icons.push(0);
        this.nav(-1)
    };
    h["app.TabLibrary"] = pa;
    pa.__name__ = !0;
    pa.get_inst = function() {
        null == pa._inst && (pa._inst = new pa);
        return pa._inst
    };
    pa.__super__ = r;
    pa.prototype = w(r.prototype, {
        updateLang: function() {
            this.lbSearch.locTextTooltip("tab.library", "search");
            this.lbSearch.chainX(this.fdSearch);
            this.rootDir.updateLang();
            this.navSync();
            r.prototype.updateLang.call(this)
        },
        search: function(a) {
            var b = this,
                c = function(a, c) {
                    null == b.dirSearch.nodes[a] ? (a = 0 < b.shcSearch.length ? b.shcSearch.pop() : new mb(0, "", []), b.dirSearch.nodes.push(a)) : a = b.dirSearch.nodes[a];
                    a.name = c;
                    return a
                },
                d;
            for (d = this.searchResults.length; 0 < d;) this.searchResults.pop(), d--;
            var e = 0;
            if (0 < a.length) {
                var f = null;
                a.split(",");
                e = A.list;
                d = 0;
                for (var g = a.split(","); d < g.length;) {
                    var m = g[d];
                    ++d;
                    m = D.trim(m).toLowerCase();
                    if (!(2 > m.length)) {
                        var x =
                            y.cca(m, 0);
                        if (35 == x) {
                            m = m.substring(1);
                            var h = m.indexOf("-");
                            if (-1 != h) {
                                var k = B.parseInt(m.substring(0, h));
                                if (null != k && (x = k, k = B.parseInt(m.substring(h + 1)), null != k))
                                    for (m = x; m <= k;) this.searchResults.push(m), m++
                            } else k = B.parseInt(m), null != k && this.searchResults.push(k)
                        } else {
                            46 == x && (m = m.substring(1));
                            k = m.split(" ");
                            for (m = k.length; 0 <= --m;) 0 == k[m].length && k.splice(m, 1);
                            if (46 == x)
                                for (x = 0; x < e.length;) {
                                    h = e[x];
                                    var l = h.textLq;
                                    for (m = k.length; 0 <= --m && !(0 > l.indexOf(k[m])););
                                    0 > m && b.searchResults.push(h.id);
                                    x++
                                } else
                                    for (x =
                                        0; x < e.length;) {
                                        h = e[x];
                                        l = h.nameLq;
                                        for (m = k.length; 0 <= --m && !(0 > l.indexOf(k[m])););
                                        0 > m && b.searchResults.push(h.id);
                                        x++
                                    }
                        }
                    }
                }
                d = this.searchResults.length;
                for (m = 0; m < this.searchResults.length;) 0 == m % 40 && (x = m / 40 | 0, f = m < d - 40 ? c(x, m + 1 + "-" + (m + 40)) : c(x, m + 1 + "-" + d)), f.nodes[m % 40] = this.searchResults[m], m++;
                e = d;
                0 == d && (null == this.dirSearch.nodes[0] ? (f = 0 < this.shcSearch.length ? this.shcSearch.pop() : new mb(0, "", []), this.dirSearch.nodes.push(f)) : f = this.dirSearch.nodes[0], f.name = "No results", f.nodes[0] = 0, d++);
                for (; 0 != d % 40;) f.nodes[d++ %
                    40] = 0;
                for (c = this.dirSearch.nodes.length - (d / 40 | 0); 0 <= --c;) this.shcSearch.push(this.dirSearch.nodes.pop());
                this.lib != this.dirSearch && (this.stack.push(this.lib), this.lib = this.dirSearch);
                this.dirSearch.name = "Search (" + e + ")";
                this.nav(1, this)
            } else this.dirSearch.name = "Search", this.lib == this.dirSearch && (this.lib = this.stack.pop(), this.nav(-1, this));
            c = T.inst;
            c.tabNow == c.tabSearch && (c.lbSearch[0].set_text("" + e + ' search results for "'), c.lbSearch[1].set_text(a), a = c.lbSearch[0], c.lbSearch[1].x = a.x + a.width, a =
                c.lbSearch[1], c.lbSearch[2].x = a.x + a.width);
            return e
        },
        drawItem: function(a, b, c) {
            var calIt = A.idMap.h[c];
            if (calIt && calIt.calImg) {
                window.__calamityDrawIcon(this.m.context, calIt.calImg, a, b, 20);
                return
            }
            0 != c && (0 < c && c < I.ITEMS ? this.m.context.drawImage(this.m.imgItems, 40 * (c & 31), 40 * (c >> 5), 40, 40, a, b, 20, 20) : 0 > c && c > I.NITEMS || this.m.xrect(a + 3, b + 3, 14, 14, 4933764, 1))
        },
        getNav: function(a) {
            var b = this;
            return function(c) {
                b.nav(a, null, b.m.shiftDown)
            }
        },
        nav: function(a, b, c) {
            var d;
            var e = a;
            1 < this.stack.length && --e;
            if (0 > e) 1 < this.stack.length && (this.lib = this.stack.pop());
            else if (e < this.lib.nodes.length) switch (e = this.lib.nodes[e], e.type) {
                case 1:
                    this.stack.push(this.lib);
                    this.lib = e;
                    break;
                case 2:
                    this.selected = e;
                    Ca.get_inst().load(this.selected.nodes);
                    e = -1;
                    for (d = this.lines.length; ++e < d;) this.lines[e].set_style(a == e ? 4 : 1);
                    if (c && (a = T.inst.tabNow, J.__instanceof(a, Fa)))
                        for (c = Ca.get_inst(), e = a.items.length, e > c.items.length && (e = c.items.length), d = 0; d < e;) {
                            var f = d++;
                            a.items[f].set_id(c.items[f].slot.item);
                            a.items[f].set_count(c.items[f].get_count());
                            a.items[f].set_prefix(c.items[f].get_prefix());
                            O.applyBestPrefix(a.items[f])
                        }
                    if (null == b) return
            } else if (null == b) return;
            this.navSync()
        },
        navSync: function() {
            var a =
                0;
            1 < this.stack.length && (this.icons[a] = 0, this.lines[a++].set_text("../" + this.lib.name));
            for (var b = -1, c = this.lib.nodes.length; ++b < c && !(this.icons[a] = this.lib.nodes[b].icon, this.lines[a].set_style(this.lib.nodes[b] == this.selected ? 4 : 1), this.lines[a++].set_text("  " + this.lib.nodes[b].name), a >= this.lines.length););
            for (; a < this.lines.length;) this.icons[a] = 0, this.lines[a].set_style(1), this.lines[a++].set_text("")
        },
        render: function(a, b) {
            var c, d = this.lines.length;
            for (c = -1; ++c < d;) this.drawItem(a + this.lines[c].x -
                8, b + this.lines[c].y, this.icons[c]);
            r.prototype.render.call(this, a, b)
        },
        __class__: pa
    });
    var ha = function() {};
    h["app.Shelf"] = ha;
    ha.__name__ = !0;
    ha.prototype = {
        updateLang: function() {
            if ("" != this.enName && "(< 0)" != this.enName && !ha.rxNum.match(this.enName))
                if (ha.rxPage.match(this.enName)) {
                    var a = "Page $1";
                    a = l.loc("lib.item", a, a);
                    this.name = D.replace(a, "$1", ha.rxPage.matched(1))
                } else ha.rxPages.match(this.enName) ? (a = "Pages $1+", a = l.loc("lib.item", a, a), this.name = D.replace(a, "$1", ha.rxPages.matched(1))) : ha.rxAuto.match(this.enName) ?
                    (a = ha.rxAuto.matched(1) + "($1)", a = l.loc("lib.item", a, a), this.name = a = D.replace(a, "$1", ha.rxAuto.matched(2))) : this.name = l.loc("lib.item", this.enName, this.enName)
        },
        __class__: ha
    };
    var ub = function(a, b, c) {
        this.icon = a;
        this.type = 1;
        this.name = this.enName = b;
        this.nodes = c
    };
    h["app.ShDir"] = ub;
    ub.__name__ = !0;
    ub.__super__ = ha;
    ub.prototype = w(ha.prototype, {
        updateLang: function() {
            ha.prototype.updateLang.call(this);
            for (var a = 0, b = this.nodes; a < b.length;) {
                var c = b[a];
                ++a;
                c.updateLang()
            }
        },
        __class__: ub
    });
    var mb = function(a, b, c) {
        this.icon =
            a;
        this.type = 2;
        this.name = this.enName = b;
        this.nodes = c
    };
    h["app.ShItems"] = mb;
    mb.__name__ = !0;
    mb.__super__ = ha;
    mb.prototype = w(ha.prototype, {
        __class__: mb
    });
    var Sa = function() {
        this.btHardcoreTimeout = 0;
        var a = this;
        r.call(this);
        var b;
        this.parts = [];
        this.parts[0] = new qa(40, 56, [new ba(80, 0)]);
        this.parts[1] = new qa(40, 56, [new ba(0, 0)]);
        this.parts[2] = new qa(40, 56, [new ba(40, 0)]);
        this.parts[3] = new qa(40, 56, [new ba(480, 0), new ba(120, 0)]);
        this.parts[4] = new qa(40, 56, [new ba(320, 0), new ba(160, 0)]);
        this.parts[5] = new qa(40,
            56, [new ba(360, 0), new ba(200, 0)]);
        this.parts[6] = new qa(40, 56, [new ba(400, 0), new ba(240, 0)]);
        this.parts[7] = new qa(40, 56, [new ba(440, 0), new ba(280, 0)]);
        var c = [];
        for (b = -1; 134 > ++b;) c.push(new ba(40 * (b & 15), 56 + 40 * (b >> 4)));
        this.parts[8] = new qa(40, 40, c);
        this.tabVersion = new ya(this);
        this.tabFlags = new Vb(this);
        this.lbVersion = [];
        this.lbVersion[0] = this.addLabel(0, 0, "Format: ", 7);
        this.lbVersion[1] = this.addLabel(function(a) {
            a = a.lbVersion[0];
            return a.x + a.width
        }(this), 0, "1.0.0", 1);
        this.lbVersion[2] = this.addLabel(function(a) {
            a =
                a.lbVersion[1];
            return a.x + a.width
        }(this), 0, " (", 1);
        this.btChangeVer = this.addButton(function(a) {
            a = a.lbVersion[2];
            return a.x + a.width
        }(this), 0, "Change", 4, function(b) {
            T.inst.setTab(a.tabVersion)
        });
        this.lbVersion[3] = this.addLabel(function(a) {
            a = a.btChangeVer;
            return a.x + a.width
        }(this), 0, ")", 1);
        b = this.lbVersion[2];
        b = b.y + b.height;
        this.lbDiff = this.addLabel(0, b, "Mode: ", 3);
        c = this.lbDiff.x + this.lbDiff.width;
        this.btDiff = [];
        this.btDiff[3] = this.addButton(c, b, "Journey", 1, function(b) {
            if (3 != a.m.player.difficulty) {
                a.m.player.difficulty =
                    3;
                for (b = 0; b < a.btDiff.length;) a.btDiff[b].set_style(3 == b ? 4 : 1), b++;
                a.btHardcore.set_text("")
            }
        });
        c = function(a) {
            a = a.btDiff[3];
            return a.x + a.width
        }(this) + 8;
        this.btDiff[0] = this.addButton(c, b, "Softcore", 1, function(b) {
            if (0 != a.m.player.difficulty) {
                for (b = a.m.player.difficulty = 0; b < a.btDiff.length;) a.btDiff[b].set_style(0 == b ? 4 : 1), b++;
                a.btHardcore.set_text("")
            }
        });
        c = function(a) {
            a = a.btDiff[0];
            return a.x + a.width
        }(this) + 8;
        this.btDiff[1] = this.addButton(c, b, "Mediumcore", 1, function(b) {
            if (1 != a.m.player.difficulty) {
                a.m.player.difficulty =
                    1;
                for (b = 0; b < a.btDiff.length;) a.btDiff[b].set_style(1 == b ? 4 : 1), b++;
                a.btHardcore.set_text("")
            }
        });
        c = function(a) {
            a = a.btDiff[1];
            return a.x + a.width
        }(this) + 8;
        this.btDiff[2] = this.addButton(c, b, "Hardcore", 1, function(b) {
            2 != a.m.player.difficulty && (b = a.btDiff[2], a.btHardcore.x = b.x + b.width + 8, a.btHardcore.set_text(Ja.confirmText), a.btHardcoreTimeout = a.m.time + 7)
        });
        c = function(a) {
            a = a.btDiff[2];
            return a.x + a.width
        }(this) + 8;
        this.btHardcore = this.addButton(c, b, "", 2, function(b) {
            if (2 != a.m.player.difficulty) {
                a.m.player.difficulty =
                    2;
                for (b = 0; b < a.btDiff.length;) a.btDiff[b].set_style(2 == b ? 4 : 1), b++;
                a.btHardcore.set_text("")
            }
        });
        this.lbHp1 = this.addLabel(0, function(a) {
            a = a.lbDiff;
            return a.y + a.height
        }(this), "Health: ", 4);
        this.lbHp2 = this.addLabel(0, this.lbHp1.y, "/", 1);
        (this.fdHpMax = this.addInt(0, this.lbHp1.y)).onFinish = function(b) {
            a.m.player.healthMax = b
        };
        (this.fdHpNow = this.addInt(this.lbHp1.x + this.lbHp1.width, this.lbHp1.y)).onChange = function(b) {
            a.lbHp2.x = a.fdHpNow.x + a.fdHpNow.width;
            a.fdHpMax.x = a.lbHp2.x + a.lbHp2.width
        };
        this.fdHpNow.onFinish =
            function(b) {
                a.m.player.healthNow = b
            };
        this.lbMp1 = this.addLabel(240, this.lbHp1.y, "Mana: ", 5);
        this.lbMp2 = this.addLabel(0, this.lbMp1.y, "/");
        (this.fdMpMax = this.addInt(0, this.lbMp1.y)).onFinish = function(b) {
            a.m.player.manaMax = b
        };
        (this.fdMpNow = this.addInt(this.lbMp1.x + this.lbMp1.width, this.lbMp1.y)).onChange = function(b) {
            a.lbMp2.x = a.fdMpNow.x + a.fdMpNow.width;
            a.fdMpMax.x = a.lbMp2.x + a.lbMp2.width
        };
        this.fdMpNow.onFinish = function(b) {
            a.m.player.manaNow = b
        };
        this.lbHair = this.addLabel(0, function(a) {
            a = a.lbHp1;
            return a.y +
                a.height
        }(this) + 8, "Hair style: ", 1);
        this.btHairPrev = this.addButton(function(a) {
            a = a.lbHair;
            return a.x + a.width
        }(this), this.lbHair.y, "<", 4, function(b) {
            b = (a.m.player.hairStyle - 1) % 134;
            0 > b && (b += 134);
            a.parts[8].set_style(a.m.player.hairStyle = a.fdHair.set_value(b))
        });
        this.fdHair = this.addInt(function(a) {
            a = a.btHairPrev;
            return a.x + a.width
        }(this) + 20, this.lbHair.y);
        this.fdHair.set_halign(1);
        this.fdHair.onFinish = function(b) {
            a.parts[8].set_style(a.m.player.hairStyle = b)
        };
        this.fdHair.onChange = function(b) {
            a.parts[8].set_style(B.parseInt(b))
        };
        this.btHairNext = this.addButton(function(a) {
            a = a.btHairPrev;
            return a.x + a.width
        }(this) + 40, this.lbHair.y, ">", 4, function(b) {
            b = (a.m.player.hairStyle + 1) % 134;
            0 > b && (b += 134);
            a.parts[8].set_style(a.m.player.hairStyle = a.fdHair.set_value(b))
        });
        this.lbStyle = this.addLabel(0, function(a) {
            a = a.lbHair;
            return a.y + a.height
        }(this), "Gender+Clothes: ", 3);
        this.fdStyle = this.addInt(function(a) {
            a = a.lbStyle;
            return a.x + a.width
        }(this), this.lbStyle.y, "$", 1, function(b) {
            a.m.player.gender = b
        });
        b = this.lbStyleNote = this.addLabel(0, function(a) {
            a =
                a.lbStyle;
            return a.y + a.height
        }(this), "(preview is broken atm)");
        c = b.y + b.height;
        this.lbColor = [];
        this.fdColor = [];
        var d = Sa.ftColor;
        for (b = -1; 7 > ++b;) this.lbColor[b] = this.addLabel(0, c, d[b] + ": "), this.fdColor[b] = this.addColor(this.lbColor[b].width, c), c += this.lbColor[b].height;
        this.fdColor[0].onFinish = function(b) {
            a.parts[8].set_color(a.m.player.hairColor = b)
        };
        this.fdColor[1].onFinish = function(b) {
            a.parts[3].set_color(a.parts[1].set_color(a.m.player.skinColor = b))
        };
        this.fdColor[2].onFinish = function(b) {
            a.parts[2].set_color(a.m.player.eyeColor =
                b)
        };
        this.fdColor[3].onFinish = function(b) {
            a.parts[4].set_color(a.m.player.shirtColor = b)
        };
        this.fdColor[4].onFinish = function(b) {
            a.parts[5].set_color(a.m.player.underColor = b)
        };
        this.fdColor[5].onFinish = function(b) {
            a.parts[6].set_color(a.m.player.pantsColor = b)
        };
        this.fdColor[6].onFinish = function(b) {
            a.parts[7].set_color(a.m.player.shoesColor = b)
        };
        this.lbQuests = this.addLabel(240, function(a) {
            a = a.lbHp1;
            return a.y + a.height
        }(this) + 8, "Fishing quests complete: ", 5);
        this.fdQuests = this.addInt(function(a) {
            a = a.lbQuests;
            return a.x + a.width
        }(this), this.lbQuests.y, "$", 1, function(b) {
            a.m.player.fishingQuestsCompleted = b
        });
        this.lbGolfScore = this.addLabel(240, function(a) {
            a = a.lbQuests;
            return a.y + a.height
        }(this), "Golf score: ", 8);
        this.fdGolfScore = this.addInt(function(a) {
            a = a.lbGolfScore;
            return a.x + a.width
        }(this), this.lbGolfScore.y, "$", 1, function(b) {
            a.m.player.golferScore = b
        });
        this.btFlags = this.addButton(240, function(a) {
            a = a.lbGolfScore;
            return a.y + a.height
        }(this), "Edit permanent buffs", 4, function(b) {
            T.inst.setTab(a.tabFlags)
        });
        this.lbBarQuests = this.addLabel(240, function(a) {
            a = a.btFlags;
            return a.y + a.height
        }(this), "Bartender quests: ", 3);
        this.fdBarQuests = this.addInt(function(a) {
            a = a.lbBarQuests;
            return a.x + a.width
        }(this), this.lbBarQuests.y, "$", 1, function(b) {
            a.m.player.bartenderQuests = b
        });
        this.remove(this.lbBarQuests);
        this.remove(this.fdBarQuests);
        this.lbPlayTimeSeconds = this.addLabel(240, function(a) {
            a = a.btFlags;
            return a.y + a.height
        }(this), "Playtime Seconds: ", 2);
        this.fdPlayTimeSeconds = this.addInt(function(a) {
            a = a.lbPlayTimeSeconds;
            return a.x + a.width
        }(this), this.lbPlayTimeSeconds.y, "$", 1, function(b) {
            a.m.player.playTimeSeconds = b;
            a.m.player.playTimeChanged = !0
        });
        this.lbPlayTimeTicks = this.addLabel(240, function(a) {
            a = a.lbPlayTimeSeconds;
            return a.y + a.height
        }(this), "Playtime Ticks: ", 2);
        this.lbPlayTimeTicks.tooltipEnText = "There are 10 million 'ticks' in one second.";
        this.fdPlayTimeTicks = this.addInt(function(a) {
            a = a.lbPlayTimeTicks;
            return a.x + a.width
        }(this), this.lbPlayTimeTicks.y, "$", 1, function(b) {
            a.m.player.playTimeTicks = b;
            a.m.player.playTimeChanged = !0
        })
    };
    h["app.TabMain"] = Sa;
    Sa.__name__ = !0;
    Sa.__super__ = r;
    Sa.prototype = w(r.prototype, {
        syncVersionLabels: function() {
            var a = this.lbVersion[0];
            this.lbVersion[1].x = a.x + a.width;
            a = this.lbVersion[1];
            this.lbVersion[2].x = a.x + a.width;
            a = this.lbVersion[2];
            this.btChangeVer.x = a.x + a.width;
            a = this.btChangeVer;
            this.lbVersion[3].x = a.x + a.width
        },
        updateBMF: function() {
            r.prototype.updateBMF.call(this);
            this.tabVersion.updateBMF();
            this.tabFlags.updateBMF()
        },
        updateLang: function() {
            this.tabVersion.updateLang();
            this.tabFlags.updateLang();
            this.lbVersion[0].locTextTooltip("tab.char", "version");
            this.btChangeVer.locTextTooltip("tab.char", "changeVersion");
            this.syncVersionLabels();
            this.lbDiff.locTextTooltip("tab.char", "mode");
            for (var a = this.btDiff, b = 0, c = a.length; b < c;) {
                var d = b++;
                a[d].locTextTooltip("tab.char", "mode" + d)
            }
            this.lbDiff.chainX(a[3], 8).chainX(a[0], 8).chainX(a[1], 8).chainX(a[2], 8);
            this.lbHp1.locTextTooltip("tab.char", "health");
            this.lbHp1.chainX(this.fdHpNow).chainX(this.lbHp2).chainX(this.fdHpMax);
            this.lbMp1.locTextTooltip("tab.char",
                "mana");
            this.lbMp1.chainX(this.fdMpNow).chainX(this.lbMp2).chainX(this.fdMpMax);
            this.lbHair.locTextTooltip("tab.char", "hairStyle");
            this.lbHair.chainX(this.btHairPrev).chainX(this.fdHair, 20);
            this.btHairPrev.chainX(this.btHairNext, 40);
            this.lbStyle.locTextTooltip("tab.char", "bodyStyle");
            this.lbStyle.chainX(this.fdStyle);
            this.lbStyleNote.locTextTooltip("tab.char", "bodyStyleNote");
            a = 0;
            for (b = this.lbColor.length; a < b;) c = a++, this.lbColor[c].locTextTooltip("tab.char", Sa.ftColor[c].toLowerCase() + "Color"), this.lbColor[c].chainX(this.fdColor[c]);
            this.lbQuests.locTextTooltip("tab.char", "fishingQuests");
            this.lbQuests.chainX(this.fdQuests);
            this.btFlags.locTextTooltip("tab.char", "editFlags");
            this.lbBarQuests.locTextTooltip("tab.char", "bartenderQuests");
            this.lbBarQuests.chainX(this.fdBarQuests);
            this.lbGolfScore.locTextTooltip("tab.char", "golfScore");
            this.lbGolfScore.chainX(this.fdGolfScore);
            this.lbPlayTimeSeconds.locTextTooltip("tab.char", "playTimeSeconds");
            this.lbPlayTimeSeconds.chainX(this.fdPlayTimeSeconds);
            this.lbPlayTimeTicks.locTextTooltip("tab.char",
                "playTimeTicks");
            this.lbPlayTimeTicks.chainX(this.fdPlayTimeTicks);
            r.prototype.updateLang.call(this)
        },
        render: function(a, b) {
            "" != this.btHardcore.text && this.m.time > this.btHardcoreTimeout && this.btHardcore.set_text("");
            r.prototype.render.call(this, a, b)
        },
        sync: function(a) {
            var b;
            this.tabVersion.fdRaw.set_value(a.version);
            this.tabVersion.syncVersion(a.version);
            this.tabFlags.sync(a);
            this.fdHpNow.set_value(a.healthNow);
            this.fdHpMax.set_value(a.healthMax);
            this.fdMpNow.set_value(a.manaNow);
            this.fdMpMax.set_value(a.manaMax);
            var c = a.difficulty;
            for (b = 0; b < this.btDiff.length;) this.btDiff[b].set_style(c == b ? 4 : 1), b++;
            this.fdStyle.set_value(a.gender);
            c = 0;
            for (b = Sa.PART_GENDER; c < b.length;) {
                var d = b[c];
                ++c;
                this.parts[d].set_style(4 <= a.gender ? 0 : 1)
            }
            this.fdColor[0].set_value(c = a.hairColor);
            this.parts[8].set_color(c);
            this.fdColor[1].set_value(c = a.skinColor);
            this.parts[3].set_color(this.parts[1].set_color(c));
            this.fdColor[2].set_value(c = a.eyeColor);
            this.parts[2].set_color(c);
            this.fdColor[3].set_value(c = a.shirtColor);
            this.parts[4].set_color(c);
            this.fdColor[4].set_value(c = a.underColor);
            this.parts[5].set_color(c);
            this.fdColor[5].set_value(c = a.pantsColor);
            this.parts[6].set_color(c);
            this.fdColor[6].set_value(c = a.shoesColor);
            this.parts[7].set_color(c);
            this.fdHair.set_value(a.hairStyle);
            this.parts[8].set_style(a.hairStyle);
            this.fdQuests.set_value(a.fishingQuestsCompleted);
            this.fdBarQuests.set_value(a.bartenderQuests);
            this.fdGolfScore.set_value(a.golferScore);
            this.fdPlayTimeSeconds.set_value(a.playTimeSeconds);
            this.fdPlayTimeTicks.set_value(a.playTimeTicks);
            c = a.difficulty;
            0 <= c && 4 > c && this.btDiff[a.difficulty].click()
        },
        __class__: Sa
    });
    var ba = function(a, b) {
        this.x = null != a ? a : 0;
        this.y = null != b ? b : 0
    };
    h["openfl.geom.Point"] = ba;
    ba.__name__ = !0;
    ba.prototype = {
        setTo: function(a, b) {
            this.x = a;
            this.y = b
        },
        __class__: ba
    };
    var qa = function(a, b, c) {
        this.style = 0;
        this.color = 16777215;
        ca.call(this, a, b, !0, 0);
        this.source = I._main.bitPlayer;
        this._rect = new Ia(0, 0, a, b);
        this.styles = [];
        this.ctf = new jb;
        for (var d = 0; d < c.length;) {
            var e = c[d];
            ++d;
            this.styles.push(new Ia(e.x, e.y, a, b))
        }
        this.update()
    };
    h["app.BitPart"] = qa;
    qa.__name__ = !0;
    qa.__super__ = ca;
    qa.prototype = w(ca.prototype, {
        set_color: function(a) {
            this.color != a && (this.color = a, this.ctf.redMultiplier = (a >> 16 & 255) / 255, this.ctf.greenMultiplier = (a >> 8 & 255) / 255, this.ctf.blueMultiplier = (a & 255) / 255, this.update());
            return a
        },
        set_style: function(a) {
            this.style != a && (this.style = a, this.update());
            return a
        },
        update: function() {
            var a = this.style;
            if (0 > a || a >= this.styles.length) a = 0;
            this.copyPixels(this.source, this.styles[a], qa.nullPoint, null, null, !1);
            this.colorTransform(this._rect,
                this.ctf)
        },
        __class__: qa
    });
    var Yb = function() {
        this.slotLabels = [];
        p.call(this, null, 0, "Misc");
        for (var a = ["Pet", "Light pet", "Minecart", "Mount", "Hook"], b = 0; 5 > b;) {
            var c = [b++];
            this.addSlot(function(a) {
                return function(b) {
                    return b.equipmentDyes[a[0]]
                }
            }(c), 40, 40 * c[0]).color = 11104400;
            this.addSlot(function(a) {
                return function(b) {
                    return b.equipmentItems[a[0]]
                }
            }(c), 80, 40 * c[0]);
            this.slotLabels[c[0]] = this.addLabel(120, 40 * c[0] + 20, a[c[0]], 1, 0, 1)
        }
    };
    h["app.TabMiscEquips"] = Yb;
    Yb.__name__ = !0;
    Yb.__super__ = p;
    Yb.prototype =
        w(p.prototype, {
            updateLang: function() {
                this.slotLabels[0].locTextTooltip("tab.tools", "pet");
                this.slotLabels[1].locTextTooltip("tab.tools", "lightPet");
                this.slotLabels[2].locTextTooltip("tab.tools", "minecart");
                this.slotLabels[3].locTextTooltip("tab.tools", "mount");
                this.slotLabels[4].locTextTooltip("tab.tools", "hook")
            },
            extraNodes: function() {
                this.shelfCtr.y = 200
            },
            __class__: Yb
        });
    var db = function() {
        this.itemIndexOffset = 0;
        p.call(this, function(a) {
            return function(b) {
                return b.researchDummies[a]
            }
        }, 40, "", 20, 2);
        for (var a =
                0, b = this.items; a < b.length;) {
            var c = b[a];
            ++a;
            c.color = 6332544
        }
    };
    h["app.TabResearch"] = db;
    db.__name__ = !0;
    db.__super__ = p;
    db.prototype = w(p.prototype, {
        extraNodes: function() {
            var a = this;
            this.btRemoveAll = this.addConfirm(4, 0, "Remove All", 2, function(b) {
                a.m.player.researchSlots = [];
                a.setPage(1)
            });
            this.btUnlockAll = W.setTooltip(this.addConfirm(function(a) {
                a = a.btRemoveAll;
                return a.x + a.width
            }(this) + 8, 0, "Unlock All", 5, function(b) {
                b = a.m.player.researchSlots = [];
                for (var c = 0, d = A.list; c < d.length;) {
                    var e = d[c];
                    ++c;
                    if (0 != e.id) {
                        var f =
                            db.goalPerItem.h[e.id];
                        if (!(null == f || 0 > f)) {
                            var h = new na;
                            h.favFlagMinVersion = 0;
                            h.multi = !0;
                            h.item = e;
                            h.count = f;
                            b.push(h)
                        }
                    }
                }
                a.setPage(1)
            }), "ATTENTION:\nThis will add 5000 items to your research");
            var b = this.items[this.items.length - 1].y + 40;
            this.lbPage = this.addLabel(4, b, "Page: ", 1);
            this.btFirst = this.addButton(function(a) {
                a = a.lbPage;
                return a.x + a.width
            }(this), b, "[<", 4, function(b) {
                a.setPage(1)
            });
            this.btPrev = this.addButton(function(a) {
                a = a.btFirst;
                return a.x + a.width
            }(this) + 4, b, " < ", 4, function(b) {
                1 >= a.fdPage.get_value() ||
                    a.setPage(a.fdPage.get_value() - 1)
            });
            this.fdPage = this.addInt(function(a) {
                a = a.btPrev;
                return a.x + a.width
            }(this) + 20, b, "$", 1, function(b) {
                1 > b && (b = 1);
                a.itemIndexOffset = (b - 1) * a.items.length;
                a.syncPage()
            });
            this.fdPage.set_halign(1);
            this.btNext = this.addButton(function(a) {
                a = a.btPrev;
                return a.x + a.width
            }(this) + 40, b, " > ", 4, function(b) {
                a.setPage(a.fdPage.get_value() + 1)
            });
            this.btLast = W.setTooltip(this.addButton(function(a) {
                a = a.btNext;
                return a.x + a.width
            }(this) + 4, b, ">]", 4, function(b) {
                b = a.m.player.researchSlots;
                for (var c =
                        b.length; 0 <= --c;)
                    if (b[c].get_isValid()) {
                        a.setPage(1 + (c / a.items.length | 0));
                        return
                    } a.setPage(1)
            }), "Note: You can scroll past the last page");
            b = this.fdPage;
            this.shelfCtr.y = b.y + b.height;
            b = this.bottomNodes;
            b.push(this.addLabel(4, this.shelfCtr.y + 160, "Progress:", 1));
            b.push(new Zb(this.title));
            b.push(new La(this.title, !1));
            for (var c = b[0], d = 0; d < b.length;) {
                var e = b[d];
                ++d;
                e.y = c.y;
                e != c && (c = c.chainX(e, 8), this.add(e))
            }
        },
        updateLang: function() {
            this.lbPage.locTextTooltip("tab.research", "page");
            this.lbPage.chainX(this.btFirst,
                0).chainX(this.btPrev, 4).chainX(this.fdPage, 20);
            this.btPrev.chainX(this.btNext, 40).chainX(this.btLast, 4);
            this.btUnlockAll.locTextTooltip("tab.research", "unlockAll");
            this.btRemoveAll.locTextTooltip("tab.research", "removeAll");
            var a = this.btRemoveAll;
            this.btUnlockAll.x = a.x + a.width + 8;
            p.prototype.updateLang.call(this)
        },
        setPage: function(a, b) {
            this.fdPage.set_value(a);
            this.itemIndexOffset = (a - 1) * this.items.length;
            this.syncPage(b)
        },
        syncPage: function(a) {
            null == a && (a = this.m.player);
            for (var b = 0, c = this.items.length; b <
                c;) {
                var d = b++,
                    e = this.items[d],
                    f = a.researchSlots[this.itemIndexOffset + d];
                null == f && (f = new na, a.researchSlots[this.itemIndexOffset + d] = f);
                e.set_slot(f)
            }
        },
        sync: function(a) {
            this.setPage(1, a)
        },
        __class__: db
    });
    var Ha = function() {
        r.call(this);
        this.servers = [];
        this.btAdd = this.addButton(0, 0, "[+]", 4, F(this, this.addServer));
        W.setTooltip(this.btAdd, "You need to know world name+ID for this.\nUnless you are copying spawn points from an\nexisting character, there's not much use.")
    };
    h["app.TabServers"] = Ha;
    Ha.__name__ = !0;
    Ha.__super__ = r;
    Ha.prototype = w(r.prototype, {
        updateLang: function() {
            this.btAdd.tooltipText = l.ttip("tab.spawnpoints", "add", this.btAdd.tooltipEnText);
            Ha.removeTooltip = l.ttip("tab.spawnpoints", "remove", Ha.removeTooltipEn);
            for (var a = 0, b = this.servers; a < b.length;) {
                var c = b[a];
                ++a;
                c.btRemove.tooltipText = Ha.removeTooltip
            }
        },
        sync: function(a) {
            for (var b = 0, c = this.servers; b < c.length;) {
                var d = c[b];
                ++b;
                d.dispose()
            }
            this.servers = [];
            b = -1;
            for (c = a.servers.length; ++b < c;) d = new $b(this, a.servers[b]), this.servers.push(d);
            this.alignServers()
        },
        addServer: function(a) {
            a = new ac;
            var b = new $b(this, a);
            this.servers.push(b);
            this.m.player.servers.push(a);
            this.alignServers()
        },
        alignServers: function() {
            for (var a = -1, b = this.servers.length, c = 0; ++a < b;) this.servers[a].snap(c), c += this.servers[a].btRemove.height;
            this.btAdd.y = c
        },
        __class__: Ha
    });
    var $b = function(a, b) {
        var c = this,
            d;
        this.tab = a;
        this.server = b;
        this.btRemove = a.addButton(0, 0, "[x] ", 2);
        W.setTooltip(this.btRemove, Ha.removeTooltip);
        this.btRemove.onClick = function(b) {
            c.dispose();
            y.remove(a.servers, c);
            y.remove(a.m.player.servers,
                c.server);
            a.alignServers()
        };
        a.add(this.fdName = new eb);
        this.fdName.set_format('"$"');
        this.fdName.set_value(b.name);
        this.fdName.onChange = F(this, this.resync);
        this.fdName.onFinish = function(a) {
            b.name
        };
        a.add(this.fdAddr = new vb);
        this.fdAddr.set_style(3);
        this.fdAddr.onVerify = F(this, this.verifyAddr);
        this.fdAddr.set_value(b.address);
        this.fdAddr.onChange = F(this, this.resync);
        this.fdAddr.onFinish = function(a) {
            b.address = a
        };
        this.lbPos = [];
        for (d = -1; 4 > ++d;) a.add(this.lbPos[d] = new u);
        this.lbPos[0].set_text(" [");
        this.lbPos[1].set_text("] (");
        this.lbPos[2].set_text(", ");
        this.lbPos[3].set_text(")");
        this.fdX = a.addInt(0, 0, "$", 1, function(a) {
            c.server.spawnX = a
        });
        this.fdX.set_value(this.server.spawnX);
        this.fdX.onChange = F(this, this.resync);
        this.fdX.onFinish = function(a) {
            c.server.spawnX = a
        };
        this.fdY = a.addInt(0, 0, "$", 1, function(a) {
            c.server.spawnX = a
        });
        this.fdY.set_value(this.server.spawnY);
        this.fdY.onChange = F(this, this.resync);
        this.fdY.onFinish = function(a) {
            c.server.spawnY = a
        };
        this.resync(null)
    };
    h["app._TabServers.ServerNode"] = $b;
    $b.__name__ = !0;
    $b.prototype = {
        snap: function(a) {
            var b;
            this.fdAddr.y = this.btRemove.y = this.fdName.y = this.fdX.y = this.fdY.y = a;
            for (b = -1; 4 > ++b;) this.lbPos[b].y = a
        },
        dispose: function() {
            var a;
            this.tab.remove(this.btRemove);
            this.tab.remove(this.fdName);
            this.tab.remove(this.fdAddr);
            for (a = -1; 4 > ++a;) this.tab.remove(this.lbPos[a]);
            this.tab.remove(this.fdX);
            this.tab.remove(this.fdY)
        },
        verifyAddr: function(a) {
            return 0 <= a && -1 >= a
        },
        resync: function(a) {
            a = this.btRemove;
            this.fdName.x = a.x + a.width;
            a = this.fdName;
            this.lbPos[0].x = a.x + a.width;
            a = this.lbPos[0];
            this.fdAddr.x = a.x + a.width;
            a = this.fdAddr;
            this.lbPos[1].x = a.x + a.width;
            a = this.lbPos[1];
            this.fdX.x = a.x + a.width;
            a = this.fdX;
            this.lbPos[2].x = a.x + a.width;
            a = this.lbPos[2];
            this.fdY.x = a.x + a.width;
            a = this.fdY;
            this.lbPos[3].x = a.x + a.width
        },
        __class__: $b
    };
    var Ca = function() {
        Fa.call(this);
        var a;
        for (a = -1; 40 > ++a;) {
            var b = [new na],
                c = new O(function(a) {
                    return function(b) {
                        return a[0]
                    }
                }(b), 1);
            c.x = a % 10 * 40;
            c.y = 40 * (a / 10 | 0);
            c.set_slot(b[0]);
            this.add(c);
            this.items.push(c)
        }
    };
    h["app.TabShelf"] = Ca;
    Ca.__name__ = !0;
    Ca.get_inst = function() {
        null ==
            Ca._inst && (Ca._inst = new Ca);
        return Ca._inst
    };
    Ca.__super__ = Fa;
    Ca.prototype = w(Fa.prototype, {
        sync: function(a) {},
        load: function(a) {
            var b, c = a.length,
                d = this.items.length,
                e = A.fromId(0);
            for (b = -1; ++b < d;) this.items[b].set_id(b < c ? A.fromId(a[b]) : e), this.items[b].set_count(1)
        },
        __class__: Ca
    });
    var cc = function() {
        this.readMore = null;
        this.links = [];
        this.heyListenTime = 0;
        this.heyListen = null;
        var a = this;
        r.call(this);
        var b, c = function(b, c, d, e, f) {
                e = new bc(e);
                e.x = b;
                e.y = c;
                e.set_style(f);
                e.set_text(e.enText = d);
                a.add(e);
                return e
            },
            d = [];
        this.labels = d;
        var e = [];
        this.links = e;
        var f = null;
        d[0] = f = this.addLabel(0, 0, 'Click "Load player" to load a PLR file.', 4);
        d[1] = f = this.addLabel(0, f.y + f.height, "These are most commonly found in:");
        d[2] = f = this.addLabel(0, f.y + f.height, "Windows", 4);
        d[3] = f = this.addLabel(f.x + f.width, f.y, ': "My Documents/My Games/Terraria/Players"');
        d[4] = f = this.addLabel(0, f.y + f.height, "Mac", 4);
        d[5] = f = this.addLabel(f.x + f.width, f.y, ': "~/Library/Application Support/Terraria/Players" (user home)');
        d[6] = f = this.addLabel(0, f.y +
            f.height, "Linux", 4);
        d[7] = f = this.addLabel(f.x + f.width, f.y, ': "~/.local/share/Terraria/Players" (or "$XDG_DATA_HOME/Terraria/Players")');
        d[8] = f = this.addLabel(0, f.y + f.height + 10, "Terrasavr is made by ");
        e.push(this.linkYAL = b = c(f.x + f.width, f.y, "YellowAfterlife", "https://yal.cc", 3));
        d[9] = f = this.addLabel(b.x + b.width, f.y, "!");
        this.langCredit = f = this.addLabel(0, f.y + f.height, "");
        d[10] = f = this.addLabel(0, f.y + f.height + 10, "Last updated on ");
        this.labelDate = f = this.addLabel(f.x + f.width, f.y, "August 22, 2026", 4);
        d[11] =
            f = this.addLabel(f.x + f.width, f.y, " (for Terraria ");
        this.labelVersion = f = this.addLabel(f.x + f.width, f.y, "1.4.5.7", 4);
        d[12] = f = this.addLabel(f.x + f.width, f.y, "):");
        this.labelChanges = f = this.addLabel(0, f.y + f.height, "- Unofficial Beta fork with a local patch for Terraria 1.4.5.7 saves\n- Fully offline, updated translations, Builds & What's-new panels\n- See the info button at the bottom of the window for details", 1);
        e.push(this.readMore = c(0, f.y + f.height, "Read more >>>", "https://yellowafterlife.itch.io/terrasavr/devlog/1609388/french-localization-and-new-prefixes", 4))
    };
    h["app.TabStart"] = cc;
    cc.__name__ = !0;
    cc.__super__ = r;
    cc.prototype = w(r.prototype, {
        update: function(a) {
            r.prototype.update.call(this, a);
            null != this.heyListen && (this.heyListenTime += a / 1.7, this.heyListen.y = B["int"](this.heyListenY + 3 * Math.sin(this.heyListenTime * Math.PI * 2)))
        },
        end: function() {
            r.prototype.end.call(this);
            for (var a = 0, b = this.links; a < b.length;) {
                var c = b[a];
                ++a;
                c.set_enabled(!1)
            }
        },
        updateLang: function() {
            var a = this.labels;
            var b = 0;
            for (var c = a.length; b < c;) {
                var d = b++;
                a[d].locTextTooltip("tab.intro", "label" + d)
            }
            b = a[0];
            a[1].y = b.y + b.height;
            a[2].y =
                function(b) {
                    b = a[1];
                    return a[3].y = b.y + b.height
                }(this);
            a[2].chainX(a[3]);
            a[4].y = function(b) {
                b = a[2];
                return a[5].y = b.y + b.height
            }(this);
            a[4].chainX(a[5]);
            a[6].y = function(b) {
                b = a[4];
                return a[7].y = b.y + b.height
            }(this);
            a[6].chainX(a[7]);
            b = function(b) {
                b = a[6];
                return b.y + b.height
            }(this) + 10;
            a[8].y = this.linkYAL.y = a[9].y = b;
            a[8].chainX(this.linkYAL).chainX(a[9]);
            b = a[8];
            this.langCredit.y = b.y + b.height;
            this.langCredit.set_text(l.loc("tab.intro", "aboutTranslator", this.langCredit.enText));
            b = function(a) {
                a = a.langCredit;
                return a.y + a.height
            }(this) + 10;
            a[10].y = a[11].y = a[12].y = this.labelDate.y = this.labelVersion.y = b;
            a[10].chainX(this.labelDate).chainX(a[11]).chainX(this.labelVersion).chainX(a[12]);
            b = a[10];
            this.labelChanges.y = b.y + b.height;
            b = this.labelChanges;
            this.readMore.y = b.y + b.height;
            null != this.terranionPre && (this.terranionPre.y = this.linkTerranion.y = function(a) {
                var b = a.labelChanges;
                return a.terranionPost.y = b.y + b.height
            }(this), this.terranionPre.set_text(l.loc("tab.intro", "terranion0", this.terranionPre.enText)), this.linkTerranion.set_text(l.loc("tab.intro",
                "terranion1", this.linkTerranion.enText)), this.terranionPost.set_text(l.loc("tab.intro", "terranion2", this.terranionPost.enText)), this.terranionPre.chainX(this.linkTerranion).chainX(this.terranionPost));
            null != this.researchLink && (this.researchAlso.y = this.researchPre.y = this.researchLink.y = function(a) {
                var b = a.terranionPre;
                return a.researchPost.y = b.y + b.height
            }(this), this.researchAlso.set_text(l.loc("tab.intro", "research0", this.researchAlso.enText)), this.researchPre.set_text(l.loc("tab.intro", "research1",
                this.researchPre.enText)), this.researchLink.set_text(l.loc("tab.intro", "research2", this.researchLink.enText)), this.researchPost.set_text(l.loc("tab.intro", "research3", this.researchPost.enText)), this.researchAlso.chainX(this.researchPre).chainX(this.researchLink).chainX(this.researchPost));
            r.prototype.updateLang.call(this)
        },
        __class__: cc
    });
    var bc = function(a) {
        this.enabled = !0;
        R.call(this);
        this.anchor = new xb(a, "_blank");
        this.anchor.component.style.zIndex = "100";
        this.block = new yb(10, 10, 0);
        this.anchor.addChild(this.block)
    };
    h["app.TabStartLink"] = bc;
    bc.__name__ = !0;
    bc.__super__ = R;
    bc.prototype = w(R.prototype, {
        set_enabled: function(a) {
            this.enabled != a && ((this.enabled = a) || null == this.anchor.parent || this.anchor.parent.removeChild(this.anchor));
            return a
        },
        render: function(a, b) {
            R.prototype.render.call(this, a, b);
            this.enabled && null == this.anchor.parent && I._main.addChild(this.anchor);
            this.anchor.set_x(a + this.x - this.ofs_x);
            this.anchor.set_y(b + this.y - this.ofs_y);
            this.block.set_width(this.width);
            this.block.set_height(this.height)
        },
        __class__: bc
    });
    var ya = function(a) {
        this.current = null;
        this.versionButtonArr = [];
        this.versionButtonMap = new Ba;
        var b = this;
        r.call(this);
        this.tabMain = a;
        ya.pcVersions = ya.initPC();
        this.lbRaw = this.addLabel(0, 0, "Release number: ", 4);
        this.fdRaw = this.addInt(function(a) {
            a = a.lbRaw;
            return a.x + a.width
        }(this), this.lbRaw.y, "$", 1, function(a) {
            b.m.player.set_version(a);
            b.syncVersion(a)
        });
        a = this.lbRaw;
        a = a.y + a.height;
        for (var c = 0, d = ya.versionGroups; c < d.length;) {
            var e = d[c];
            ++c;
            var f = this.addLabel(0, a, e.text + ":", 4),
                g = f.x + f.width,
                m = 0;
            for (e =
                e.versions; m < e.length;) {
                var h = e[m];
                ++m;
                g = this.addButton(g + 10, a, h.text, 1, this.getVersionSetter(h.num));
                this.versionButtonMap.h[h.num] = g;
                g;
                this.versionButtonArr.push(g);
                g = g.x + g.width
            }
            a = f.y + f.height
        }
    };
    h["app.TabVersion"] = ya;
    ya.__name__ = !0;
    ya.initPC = function() {
        var a = [],
            b = [],
            c = function(c, e) {
                c = new rc(c);
                for (var d = 0; d < e.length;) {
                    var g = e[d];
                    ++d;
                    var m = g.indexOf("="),
                        h = B.parseInt(g.substring(m + 1));
                    g = new sc(g.substring(0, m), h);
                    ya.versionNameMap.set(g.text, g);
                    g;
                    a.push(g);
                    c.versions.push(g)
                }
                b.push(c)
            };
        c("1.1.x",
            ["1.1.2=39"]);
        c("1.2.x", ["1.2.0=69", "1.2.1=73", "1.2.2=77", "1.2.3=93", "1.2.4=98"]);
        c("1.3.x", ["1.3.0=145", "1.3.1=168", "1.3.3=175", "1.3.4=184", "1.3.5=190"]);
        c("1.4.x", "1.4.0=225 1.4.0.5=230 1.4.1.2=237 1.4.3.0=248 1.4.4.0=269 1.4.5.0=315".split(" "));
        ya.versionGroups = b;
        return a
    };
    ya.__super__ = r;
    ya.prototype = w(r.prototype, {
        updateLang: function() {
            r.prototype.updateLang.call(this);
            this.lbRaw.locTextTooltip("tab.version", "rawVersion");
            var a = this.lbRaw;
            this.fdRaw.x = a.x + a.width
        },
        getVersionSetter: function(a) {
            var b =
                this;
            return function(c) {
                b.fdRaw.set_value(a);
                b.fdRaw.onFinish(a)
            }
        },
        syncVersion: function(a) {
            null == a && (a = this.m.player.version);
            var b = this.findBestMatch(a);
            this.tabMain.lbVersion[1].set_text(this.getBestText(a, b));
            null != this.current && this.current.set_style(1);
            b.set_style(4);
            this.current = b;
            this.tabMain.syncVersionLabels()
        },
        findBestMatch: function(a) {
            for (var b = this.versionButtonArr.length; 0 <= --b;) {
                var c = this.versionButtonArr[b],
                    d = ya.versionNameMap.get(c.text);
                if (a >= d.num) return c
            }
            return this.versionButtonArr[0]
        },
        getBestText: function(a, b) {
            var c = b.text;
            a > ya.versionNameMap.get(b.text).num && (c += "+");
            return c
        },
        __class__: ya
    });
    var sc = function(a, b) {
        this.text = a;
        this.num = b
    };
    h["app.VersionPair"] = sc;
    sc.__name__ = !0;
    sc.prototype = {
        __class__: sc
    };
    var rc = function(a) {
        this.versions = [];
        this.text = a
    };
    h["app.VersionGroup"] = rc;
    rc.__name__ = !0;
    rc.prototype = {
        __class__: rc
    };
    var T = function() {
        this.tsLoad = 0;
        var a = this;
        T.inst = this;
        window.__tsxAPI = {
            getPlayer: function() {
                return T.inst.m.player
            },
            itemByPid: function(a) {
                a = A.pid2id.get(a);
                return null == a ? null : A.idMap.h[a]
            },
            buffNames: function() {
                return S.buffName
            },
            buffIdByName: function(a) {
                a = S.buffName.indexOf(a);
                return 0 <= a ? a : null
            },
            BuffCls: S,
            prefixIdByName: function(a) {
                for (var k in A.prefixes.h) {
                    if (A.prefixes.h[k].enName === a) return parseInt(k)
                }
                return null
            },
            ItemCls: A,
            SlotCls: na,
            getM: function() {
                return T.inst.m
            },
            getInst: function() {
                return T.inst
            },
            getLastFileName: function() {
                return T.inst.lastFile
            },
            onAfterLoad: null,
            onBeforeSave: null,
            onBeforeVanillaSave: null,
            DirCls: ub,
            ItemsCls: mb,
            getLibraryTabInst: function() {
                return pa._inst
            },
            openLibraryTabInst: function() {
                return pa.get_inst()
            },
            getResearchGoalMap: function() {
                return db.goalPerItem.h
            }
        };
        V.call(this);
        this.x = this.y = 8;
        this.fxLoad = [new Ka("Terraria player files (*.plr)", "*.plr"), new Ka("All files (*.*)",
            "*.*")];
        this.frLoad = new Ya;
        this.frLoad.addEventListener("select", function(b) {
            a.frLoad.load()
        });
        this.frLoad.addEventListener("complete", F(this, this.onLoad));
        this.frSave = new Ya;
        this.btLoad = new R;
        this.btLoad.set_style(3);
        this.btLoad.set_text(this.btLoad.enText = "Load player");
        this.btLoad.onClick = function(b) {
            b = n.getTimer();
            a.tsLoad < b && (a.tsLoad = b + 500, a.frLoad.browse(a.fxLoad))
        };
        this.add(this.btLoad);
        this.btSave = new R;
        this.btSave.set_style(4);
        this.btSave.set_text(this.btSave.enText = "Save player");
        this.btSave.x =
            function(a) {
                a = a.btLoad;
                return a.x + a.width
            }(this) + 8;
        W.setTooltip(this.btSave, "Gives you an updated PLR file.\nDon't forget to replace your PLR in\nthe Players directory by the new one!");
        this.btSave.onClick = F(this, this.onSave);
        this.add(this.btSave);
        this.fdName = new eb;
        this.fdName.set_format("[$]");
        this.fdName.set_value("Someone");
        this.fdName.x = this.btSave.x + this.btSave.width + 8;
        this.add(this.fdName);
        this.initTabs();
        this.onSync()
    };
    h["app.TopPane"] = T;
    T.__name__ = !0;
    T.__super__ = V;
    T.prototype = w(V.prototype, {
        updateLang: function() {
            this.btLoad.locTextTooltip("tab.top", "load");
            this.btSave.locTextTooltip("tab.top", "save");
            this.btStart.chainX(this.btLoad, 8).chainX(this.btSave, 8).chainX(this.fdName, 8);
            var a = function(a, b, c, d) {
                a.title = l.loc("tab.top", b, c);
                a.tooltipText = l.ttip("tab.top", b, d)
            };
            a(this.tabMain, "char", "Char.", "General values");
            a(this.tabEquips, "equips", "Equips", "Armor and accessories");
            a(this.tabMiscEquips, "tools", "Tools", "Tools");
            a(this.tabInvMain, "inventory", "Inv.", "Main inventory");
            a(this.tabPiggy,
                "bank", "Bank", "Piggy Bank");
            a(this.tabSafe, "safe", "Safe", "The bank one");
            a(this.tabForge, "forge", "Forge", "Defender's Forge");
            a(this.tabVoid, "void", "Void", "Void vault/bag");
            a(this.tabEffects, "buffs", "Buffs", "Status effects");
            a(this.tabResearch, "research", "Research", "1.4 Journey Mode research");
            a(this.tabSpawnPoints, "spawnpoints", "SPs", "Per-world spawn points");
            a = this.tabButtons[0].x;
            for (var b = 0, c = this.tabButtons; b < c.length;) {
                var d = c[b];
                ++b;
                if (d != this.btLang && d != this.btStart) {
                    var e = d.tab;
                    d.set_text(e.title);
                    d.tooltipText = e.tooltipText;
                    e.tooltipText = null;
                    d.x = a;
                    a += d.width + 8
                }
            }
            this.btHelp.x = a;
            this.btHelp.set_text(l.loc("tab.top", "help", "Help"));
            this.btHelp.tooltipText = l.ttip("tab.top", "help", "FAQs and such");
            a = 0;
            for (b = this.tabs; a < b.length;) c = b[a], ++a, c.updateLang();
            this.tabStart.updateLang()
        },
        updateBMF: function() {
            for (var a = 0, b = this.tabs; a < b.length;) {
                var c = b[a];
                ++a;
                c != this.tabNow && c.updateBMF()
            }
            this.tabStart.updateBMF();
            V.prototype.updateBMF.call(this)
        },
        setTab: function(a, b) {
            for (var c = 0, d = this.tabButtons; c <
                d.length;) {
                var e = d[c];
                ++c;
                e.set_style(e == b ? 5 : 1)
            }
            this.tabNow != a && (null != this.tabNow && (this.tabNow.end(), this.tabContainer.remove(this.tabNow)), this.tabContainer.add(this.tabNow = a), this.tabNow.start())
        },
        initTabs: function() {
            var a = this;
            this.tabContainer = new V;
            this.tabContainer.x = 8;
            this.tabContainer.y = 52;
            this.add(this.tabContainer);
            this.tabText = new r;
            this.tabText.add(this.lbText = new u);
            this.lbText.set_style(4);
            this.tabSearch = new p(null, 0, "Search");
            this.lbSearch = [this.tabSearch.addLabel(8, 0, "", 1), this.tabSearch.addLabel(8,
                0, "", 3), this.tabSearch.addLabel(8, 0, '":', 1)];
            var b = this.lbSearch[2].height;
            this.tabSearch.y = b;
            for (var c = 0, d = this.lbSearch; c < d.length;) {
                var e = d[c];
                ++c;
                e.y = -b
            }
            this.tabSearch.addLabel(8, this.tabSearch.shelfCtr.y + 160, this.lbText.text, 4);
            this.tabInvMain = new p(function(a) {
                return function(b) {
                    return b.inventory[a]
                }
            }, 50, "Inv.");
            for (b = 0; 10 > b;) c = b++, this.tabInvMain.items[c].color = 6525881;
            this.tabs = [W.setTtIcon(this.tabMain = new Sa, 58), W.setTtIcon(this.tabEquips = new Sb, 92), W.setTtIcon(this.tabMiscEquips = new Yb,
                1183), W.setTtIcon(this.tabInvMain, 50), this.tabPiggy = W.setTtIcon(new p(function(a) {
                return function(b) {
                    return b.bankItems[a]
                }
            }, 40, ""), 87), this.tabSafe = W.setTtIcon(new p(function(a) {
                return function(b) {
                    return b.safeItems[a]
                }
            }, 40, ""), 346), this.tabForge = W.setTtIcon(new p(function(a) {
                return function(b) {
                    return b.forgeItems[a]
                }
            }, 40, ""), 3813), this.tabVoid = W.setTtIcon(new p(function(a) {
                return function(b) {
                    return b.voidItems[a]
                }
            }, 40, ""), 4076), this.tabEffects = W.setTtIcon(new L, 289), this.tabResearch = W.setTtIcon(new db,
                1344), this.tabSpawnPoints = W.setTtIcon(new Ha, 224)];
            b = 4;
            this.tabButtons = [];
            c = function(b, c) {
                return function(d) {
                    a.setTab(b, c)
                }
            };
            d = 0;
            for (e = this.tabs; d < e.length;) {
                var f = e[d];
                ++d;
                var g = new nb(f);
                this.tabButtons.push(g);
                g.x = b;
                g.y = 24;
                g.set_style(f.style);
                g.set_text(f.title);
                g.tooltipText = f.tooltipText;
                f.tooltipText = null;
                g.tooltipIcon = f.tooltipIcon;
                f.tooltipIcon = 0;
                g.onClick = c(f, g);
                b += g.width + 8;
                this.add(g)
            }
            this.tabLang = new lb;
            this.tabs.push(this.tabLang);
            this.btLang = new nb(this.tabLang);
            this.btLang.set_text(this.btLang.enText =
                "English");
            this.btLang.x = 420;
            this.btLang.onClick = c(this.tabLang, this.btLang);
            this.tabButtons.push(this.btLang);
            this.btHelp = new wb("Help", window.location.protocol + "//yal.cc/r/terrasavr/doc/");
            this.btHelp.x = b;
            this.btHelp.y = 24;
            W.setTtIcon(this.btHelp, 149);
            this.add(this.btHelp);
            this.tabStart = new cc;
            this.btStart = new nb(this.tabStart);
            this.btStart.set_text(this.btStart.enText = "[>]");
            this.btStart.onClick = c(this.tabStart, this.btStart);
            this.btStart.set_style(5);
            this.tabButtons.push(this.btStart);
            this.add(this.btStart);
            this.tabContainer.add(this.tabNow = this.tabStart);
            b = cb.map.get("q");
            null != b && this.showSearch(b)
        },
        render: function(a, b) {
            var c = ja.getMaxIds(this.m.player.version);
            la.maxId = c.buff;
            O.maxId = c.item;
            c = A.idMap.h[4766];
            null != c && this.m.context.drawImage(this.m.imgItems, c.iconX, c.iconY, 40, 40, a + this.x + this.btLang.x - 22, b + this.y + this.btLang.y, 20, 20);
            V.prototype.render.call(this, a, b)
        },
        markTab: function(a) {
            for (var b = -1, c = this.tabButtons.length; ++b < c;) this.tabButtons[b].set_style(a == b ? 4 : 1)
        },
        showSearch: function(a) {
            p.sideBtns[1].click();
            pa.get_inst().fdSearch.set_value(a);
            this.tabNow != this.tabSearch && (this.markTab(-1), this.setTab(this.tabSearch));
            pa.get_inst().search(a)
        },
        print: function(a, b) {
            this.lbText.set_text("");
            this.lbText.set_style(b);
            this.lbText.set_text(a);
            this.tabNow != this.tabText && (this.markTab(-1), this.setTab(this.tabText))
        },
        onSync: function() {
            var a = this.m.player;
            this.fdName.set_value(a.name);
            for (var b = 0, c = this.tabs; b < c.length;) {
                var d = c[b];
                ++b;
                d.sync(a)
            }
        },
        loadVanilla: function(a) {
            if (0 != a.length % 16) this.print("That doesn't look like a valid character file.\nEither that or it's damaged.",
                2);
            else if (0 == a.length) this.print("The file is empty. Perhaps it was corrupt before.\nYou can try re-creating the character manually.", 2);
            else if (800 > a.length) this.print("The file is too small to be a valid character file.\nPerhaps a part is missing.", 2);
            else {
                var b = -1;
                try {
                    b = 0;
                    var c = new ma,
                        d = H.decrypt(a);
                    b = 1;
                    c.load(d);
                    this.tabNow != this.tabText && this.tabNow != this.tabSearch && this.tabNow != this.tabStart || this.tabButtons[0].click();
                    this.m.player = c;
                    this.m.tsavrOn = !0
                } catch (e) {
                    e instanceof z && (e = e.val), this.print((0 ==
                        b ? "Failed to decrypt player file" : "Failed to load player file") + ". The error was\n" + B.string(e) + "\nWas it a valid character file?", 2)
                }
            }
        },
        onLoad: function(a) {
            this.lastFile = this.frLoad.get_name();
            var lf = a.get_target();
            a = lf.data;
            this.loadVanilla(a);
            this.onSync();
            this.m.ignoreMouseEventsUntil = n.getTimer() + 500;
            if (this.m.tsavrOn && window.__tsxAPI && window.__tsxAPI.onAfterLoad) {
                try {
                    window.__tsxAPI.onAfterLoad(lf, this.lastFile)
                } catch (calEx) {
                    console.error("[calamity] onAfterLoad hook failed", calEx)
                }
            }
        },
        onSave: function(a) {
            a = this.m.player;
            a.name = this.fdName.get_value();
            var calRestore = null;
            if (window.__tsxAPI && window.__tsxAPI.onBeforeVanillaSave) {
                try {
                    calRestore = window.__tsxAPI.onBeforeVanillaSave(a)
                } catch (calEx) {
                    console.error("[calamity] onBeforeVanillaSave hook failed", calEx)
                }
            }
            var b;
            a.save(b = new Aa);
            if (calRestore) {
                try {
                    calRestore()
                } catch (calEx) {
                    console.error("[calamity] vanilla-save restore failed", calEx)
                }
            }
            b = H.encrypt(b);
            var saveName = null != this.lastFile ? this.lastFile : "" + a.name + ".plr";
            if (window.__tsxAPI && window.__tsxAPI.onBeforeSave) {
                try {
                    window.__tsxAPI.onBeforeSave(a, saveName)
                } catch (calEx) {
                    console.error("[calamity] onBeforeSave hook failed", calEx)
                }
            }
            this.frSave.save(b, saveName)
        },
        __class__: T
    });
    var nb = function(a) {
        R.call(this);
        this.tab = a
    };
    h["app.TabButton"] = nb;
    nb.__name__ = !0;
    nb.__super__ = R;
    nb.prototype = w(R.prototype, {
        __class__: nb
    });
    var Ga = function() {
        this.catGetters = {};
        this.allGetters = [];
        R.call(this)
    };
    h["app.io.IoButton"] = Ga;
    Ga.__name__ = !0;
    Ga.__super__ = R;
    Ga.prototype = w(R.prototype, {
        addGetter: function(a, b) {
            null == b && (b = "items");
            this.allGetters.push(a);
            var c = this.catGetters[b];
            null == c && (c = [], this.catGetters[b] = c);
            c.push(a)
        },
        getAllSlots: function(a) {
            return this.allGetters.map(function(b) {
                return b(a)
            })
        },
        getCatSlots: function(a) {
            for (var b = {}, c = 0, d = oa.fields(this.catGetters); c < d.length;) {
                var e = d[c];
                ++c;
                var f = this.catGetters[e].map(function(b) {
                    return b(a)
                });
                b[e] = f
            }
            return b
        },
        __class__: Ga
    });
    var U = function(a, b, c) {
        null == c && (c = "items");
        Ga.call(this);
        this.verb = c;
        this.name = a;
        this.append = b;
        this.set_style(b ? 5 : 3);
        this.set_text(b ? this.enText = "Append" : this.enText = "Load");
        W.setTooltip(this, b ? "Appends " + c + " from a previously\nsaved Terrasavr file to ones here." : "Replaces " + c + " by ones from\na previously saved Terrasavr file.");
        this.onClick = F(this,
            this.onLoad);
        null == U.frLoad && (U.frLoad = new Ya, U.fxLoad = [new Ka("Terrasavr item files (*.json;*.tsr)", "*.json;*.tsr"), new Ka("All files (*.*)", "*.*")], U.frLoad.addEventListener("select", function(a) {
            U.frLoad.load()
        }), U.frLoad.addEventListener("complete", function(a) {
            a = a.get_target().data;
            U.foLoad.onData(a)
        }))
    };
    h["app.io.IoLoad"] = U;
    U.__name__ = !0;
    U.__super__ = Ga;
    U.prototype = w(Ga.prototype, {
        onLoad: function(a) {
            U.foLoad = this;
            U.frLoad.browse(U.fxLoad)
        },
        reject: function() {
            T.inst.print("Specified file isn't a Terrasavr items file.",
                3)
        },
        onBinaryData: function(a) {
            if (12 > a.length || "/terrasavr/i" != a.readUTFBytes(12)) this.reject();
            else {
                a.readInt();
                var b = this.getAllSlots(this.m.player);
                if (!this.append)
                    for (var c = 0; c < b.length;) {
                        var d = b[c];
                        ++c;
                        d.clear()
                    }
                c = 0;
                for (d = b.length; c < d;) {
                    var e = c++;
                    try {
                        if (4 > a.length - a.position) break;
                        var f = null,
                            g = a.readInt();
                        if (0 != g) {
                            2147483647 == g && (f = a.readUTF());
                            var m = a.readInt();
                            var h = a.data.getUint8(a.position++)
                        } else m = h = 0;
                        if (this.append) {
                            var k = null;
                            for (e = 0; e < b.length;) {
                                var l = b[e];
                                ++e;
                                if (l.isEmpty()) {
                                    k = l;
                                    break
                                }
                            }
                        } else k =
                            b[e];
                        if (null == k) break;
                        var q = null != f ? A.fromCode(f) : A.fromId(g);
                        k.item = q;
                        k.count = k.multi ? m : null != q ? 1 : 0;
                        k.prefix = h
                    } catch (N) {
                        N instanceof z && (N = N.val);
                        break
                    }
                }
            }
        },
        onJsonData: function(a) {
            if ("TerrasavrItems" != a.resourceType) this.reject();
            else
                for (var b = this.getCatSlots(this.m.player), c = 0, d = oa.fields(b); c < d.length;) {
                    var e = d[c];
                    ++c;
                    ob.procItems(b[e], !1, a[e])
                }
        },
        onData: function(a) {
            a.position = 0;
            try {
                switch (a.data.getUint8(a.position++)) {
                    case 123:
                        a.position = 0;
                        try {
                            var b = JSON.parse(a.readUTFBytes(a.length));
                            this.onJsonData(b)
                        } catch (d) {
                            d instanceof
                            z && (d = d.val), this.reject()
                        }
                        break;
                    case 47:
                        a.position = 0;
                        this.onBinaryData(a);
                        break;
                    case 239:
                        if (187 == a.data.getUint8(a.position++))
                            if (191 == a.data.getUint8(a.position++)) {
                                var c = JSON.parse(a.readUTFBytes(a.length - a.position));
                                this.onJsonData(c)
                            } else this.reject();
                        else this.reject();
                        break;
                    default:
                        this.reject()
                }
            } catch (d) {
                d instanceof z && (d = d.val), T.inst.print("Error loading the file: " + B.string(d), 3)
            }
        },
        __class__: U
    });
    var Tb = function() {
        U.call(this, "Lib", !1);
        this.set_style(8);
        this.set_text(this.enText = "Library");
        this.tooltipText = "Allows to load a previously\nsaved set of items as a 'library'"
    };
    h["app.io.IoLib"] = Tb;
    Tb.__name__ = !0;
    Tb.__super__ = U;
    Tb.prototype = w(U.prototype, {
        onData: function(a) {
            U.prototype.onData.call(this, a);
            var b = Ca.get_inst();
            a.position = 0;
            if (12 > a.length || "/terrasavr/i" != a.readUTFBytes(12)) T.inst.print("Specified file isn't a Terrasavr item file.", 3);
            else {
                a.readInt();
                b = b.items;
                var c, d = b.length;
                for (c = -1; ++c < d;) {
                    var e = null;
                    if (4 <= a.length - a.position) try {
                        var f = a.readInt();
                        if (0 != f) {
                            2147483647 == f &&
                                (e = a.readUTF());
                            var g = a.readInt();
                            var m = a.data.getUint8(a.position++)
                        } else g = m = 0
                    } catch (C) {
                        C instanceof z && (C = C.val), f = g = m = 0
                    } else f = g = m = 0;
                    var h = b[c];
                    h.set_id(null != e ? A.fromCode(e) : A.fromId(f));
                    h.set_count(g);
                    h.set_prefix(m)
                }
            }
        },
        __class__: Tb
    });
    var La = function(a, b, c) {
        U.call(this, a, b, c)
    };
    h["app.io.IoLoadResearch"] = La;
    La.__name__ = !0;
    La.__super__ = U;
    La.prototype = w(U.prototype, {
        reject: function() {
            U.prototype.reject.call(this)
        },
        onBinaryData: function(a) {
            if (a.length < La.prefixLen + 8 || a.readUTFBytes(La.prefixLen) !=
                La.prefix) T.inst.print("Specified file isn't a Terrasavr item file.", 3);
            else {
                a.readInt();
                var b = a.readInt(),
                    c = this.m.player.researchSlots;
                c.splice(0, c.length);
                for (var d, e, f = 0; f < b;)
                    if (f++, d = H.readSharpString(a), e = a.readInt(), d = A.pid2id.get(d), null != d) {
                        var g = new na;
                        g.item = A.idMap.h[d];
                        g.count = e;
                        c.push(g)
                    } T.inst.tabResearch.syncPage()
            }
        },
        onJsonData: function(a) {
            if ("TerrasavrResearch" != a.resourceType) this.reject();
            else {
                a = a.research;
                var b = this.m.player.researchSlots;
                b.splice(0, b.length);
                for (var c = 0; c < a.length;) {
                    var d =
                        a[c];
                    ++c;
                    var e = new na;
                    e.item = A.idMap.h[d.id];
                    e.count = d.count;
                    b.push(e)
                }
                T.inst.tabResearch.syncPage()
            }
        },
        __class__: La
    });
    var Ra = function(a, b) {
        null == b && (b = "items");
        Ga.call(this);
        this.verb = b;
        this.set_text(this.enText = "Save");
        this.set_style(4);
        this.name = a;
        this.onClick = F(this, this.onSave);
        this.tooltipText = this.tooltipEnText = "Saves a set of items for later use.\nThis is for Terrasavr, not Terraria!"
    };
    h["app.io.IoSave"] = Ra;
    Ra.__name__ = !0;
    Ra.__super__ = Ga;
    Ra.prototype = w(Ga.prototype, {
        onSave: function(a) {
            a = new Aa;
            var b = this.getCatSlots(this.m.player),
                c = {
                    resourceType: "TerrasavrItems",
                    resourceVersion: "1.0"
                };
            c.gameVersion = this.m.player.invVersion;
            for (var d = 0, e = oa.fields(b); d < e.length;) {
                var f = e[d];
                ++d;
                var g = ob.procItems(b[f], !0, null);
                c[f] = g
            }
            a.writeUTFBytes(JSON.stringify(c, null, "\t"));
            a.set_length(a.position);
            T.inst.frSave.save(a, "" + this.name + ".json")
        },
        __class__: Ra
    });
    var Zb = function(a, b) {
        Ra.call(this, a, b)
    };
    h["app.io.IoSaveResearch"] = Zb;
    Zb.__name__ = !0;
    Zb.__super__ = Ra;
    Zb.prototype = w(Ra.prototype, {
        onSave: function(a) {
            a =
                this.m.player.researchSlots;
            var b = new Aa,
                c = {
                    resourceType: "TerrasavrResearch",
                    resourceVersion: "1.0"
                },
                d = [];
            c.research = d;
            for (var e = 0; e < a.length;) {
                var f = a[e];
                ++e;
                if (null != f.item && 0 != f.item.id) {
                    var g = {};
                    g.id = f.item.id;
                    g.count = f.count;
                    d.push(g)
                }
            }
            b.writeUTFBytes(JSON.stringify(c, null, "\t"));
            b.set_length(b.position);
            T.inst.frSave.save(b, "research.json")
        },
        __class__: Zb
    });
    var ob = function() {};
    h["app.io.JsonHelper"] = ob;
    ob.__name__ = !0;
    ob.procItem = function(a, b, c) {
        if (b) {
            if (a.isEmpty()) return null;
            c = {};
            c.id = a.item.id;
            a.multi && (c.count = a.count);
            c.prefix = a.prefix;
            0 < a.favFlagMinVersion && (c.isFavorited = a.isFavorited);
            return c
        }
        if (null == c) return a.clear(), null;
        a.item = A.fromId(c.id);
        a.multi && (a.count = c.count);
        a.prefix = c.prefix;
        0 < a.favFlagMinVersion && (a.isFavorited = c.isFavorited);
        return c
    };
    ob.procItems = function(a, b, c) {
        null == c && (c = []);
        if (b) return a.map(function(a) {
            return ob.procItem(a, !0, null)
        });
        b = 0;
        for (var d = a.length; b < d;) {
            var e = b++;
            ob.procItem(a[e], !1, c[e])
        }
        return c
    };
    var Ja = function() {
        this.isWaiting = !1;
        R.call(this)
    };
    h["dom.ButtonConfirm"] = Ja;
    Ja.__name__ = !0;
    Ja.__super__ = R;
    Ja.prototype = w(R.prototype, {
        click: function() {
            var a = this;
            if (this.isWaiting) {
                if (null != this.onClick) this.onClick(this);
                a.set_text(a.oldText);
                a.set_style(a.oldStyle);
                a.isWaiting = !1
            } else this.oldText = this.text, this.oldStyle = this.style, this.set_text(Ja.confirmText), this.set_style(1), this.isWaiting = !0, zb.delay(function() {
                a.set_text(a.oldText);
                a.set_style(a.oldStyle);
                a.isWaiting = !1
            }, 3E3)
        },
        __class__: Ja
    });
    var Mb = function() {
        this.hovering = !1;
        this.active = !0;
        this.format = "";
        this.value = !1;
        u.call(this);
        this.set_text("[ ]")
    };
    h["dom.Checkbox"] = Mb;
    Mb.__name__ = !0;
    Mb.__super__ = u;
    Mb.prototype = w(u.prototype, {
        locFormatTooltip: function(a, b) {
            this.set_format(l.loc(a, b, this.enText));
            this.tooltipText = l.ttip(a, b, this.tooltipEnText)
        },
        change: function() {
            this.set_text("[" + (this.value ? "x" : String.fromCharCode(8196)) + "] " + this.format)
        },
        hover: function(a, b) {
            this.hovering = this.active && this.hitTest(a, b) == this
        },
        render: function(a, b) {
            var c = this.active ? this.hovering ? .7 : 1 : .3;
            if (this.checkBMFont()) this.m.draw(this.cache,
                a + this.x - this.ofs_x - this.pad, b + this.y - this.ofs_y - this.pad, c);
            else {
                a += this.x;
                b += this.y;
                var d = this.m.context;
                d.save();
                d.font = u.canvasFont;
                d.textAlign = 2 == this.halign ? "right" : 1 == this.halign ? "center" : "left";
                d.textBaseline = 2 == this.valign ? "bottom" : 1 == this.valign ? "middle" : "top";
                d.shadowColor = "black";
                d.fillStyle = this.m.fontFillColors[this.style - 1];
                d.strokeStyle = "black";
                d.lineWidth = 1;
                null != c && (d.globalAlpha = c);
                c = !0;
                for (var e = 0, f = this.textLines; e < f.length;) {
                    var g = f[e];
                    ++e;
                    var m = 0;
                    c && (D.startsWith(g, "[\u2004") &&
                        (g = g.substring(2), d.shadowBlur = 4, d.strokeText("[", a + m, b + 2 * (1 - this.valign)), d.shadowBlur = 0, d.fillText("[", a + m, b + 2 * (1 - this.valign)), m = d.measureText("[x").width), c = !1);
                    d.shadowBlur = 4;
                    d.strokeText(g, a + m, b + 2 * (1 - this.valign));
                    d.shadowBlur = 0;
                    d.fillText(g, a + m, b + 2 * (1 - this.valign));
                    b += u.canvasLineHeight
                }
                d.restore()
            }
        },
        click: function() {
            this.active && this.set_value(!this.value)
        },
        set_value: function(a) {
            if (this.value != a) {
                this.value = a;
                if (null != this.onChange) this.onChange(a);
                this.change()
            }
            return a
        },
        set_format: function(a) {
            this.format !=
                a && (this.format = a, this.change());
            return a
        },
        __class__: Mb
    });
    var K = function() {
        this.active = !0;
        this.editing = this.hovering = !1;
        this._value = "";
        this.format = "$";
        u.call(this)
    };
    h["dom.VoidField"] = K;
    K.__name__ = !0;
    K.setup = function() {
        K.field = new Ab;
        K.field.set_x(K.field.set_y(-9001));
        K.field.get_defaultTextFormat().color = 16777215;
        K.field.set_height(20);
        K.field.set_multiline(!1);
        K.field.set_type("INPUT");
        n.get_current().addChild(K.field)
    };
    K.__super__ = u;
    K.prototype = w(u.prototype, {
        update: function(a) {
            this.editing &&
                this.set__value(K.field.get_text())
        },
        hover: function(a, b) {
            this.hovering = this.active && this.hitTest(a, b) == this
        },
        change: function() {
            if (null != this.onChange) this.onChange(this._value)
        },
        finish: function() {},
        render: function(a, b) {
            var c = this,
                d = 0;
            var e = 0;
            var f = this.checkBMFont();
            if (this.editing && (e = this.format.indexOf("$"), d = K.field.get_selectionBeginIndex() + e, e = K.field.get_selectionEndIndex() + e, d != e))
                if (f) this.font.select(this.text, d, e, a + this.x, b + this.y, this.halign, this.valign, function(a, b, d) {
                    c.m.xrect(a, d, b -
                        a, c.font.lineHeight, 4247807, .5)
                });
                else {
                    var g = a + this.x + this.m.textWidth(this.text.substring(0, d));
                    d = this.m.textWidth(this.text.substring(d, e));
                    this.m.xrect(g, b + this.y - 2, d, u.canvasLineHeight, 4247807, .5)
                } d = this.active ? this.hovering ? .7 : 1 : .3;
            f ? this.m.draw(this.cache, a + this.x - this.ofs_x - this.pad, b + this.y - this.ofs_y - this.pad, d) : this.m.blitText(this.textLines, a + this.x, b + this.y, this.halign, this.valign, this.style, d);
            this.editing && .7 < this.m.time % 1.4 && (f ? this.font.select(this.text, e, e, a + this.x, b + this.y, this.halign,
                this.valign,
                function(a, b, d) {
                    c.m.rect(b, d, 1, c.font.lineHeight, 16777215)
                }) : (a = a + this.x + this.m.textWidth(this.text.substring(0, e)), this.m.rect(a, b + this.y - 2, 1, this.font.lineHeight, 16777215)))
        },
        onBlur: function(a) {
            K.field.removeEventListener("blur", F(this, this.onBlur));
            K.field.removeEventListener("keydown", F(this, this.onEditKey));
            this.finish();
            this.editing = !1
        },
        onEditKey: function(a) {
            if (13 == a.keyCode) this.onBlur(null)
        },
        click: function() {
            this.editing || (this.editing = !0, null == K.field && K.setup(), K.field.addEventListener("keydown",
                F(this, this.onEditKey)), K.field.addEventListener("blur", F(this, this.onBlur)), K.field.set_text(this._value), K.field.setSelection(0, this._value.length), n.get_current().get_stage().set_focus(K.field))
        },
        set_format: function(a) {
            this.format != a && (this.format = a, this.set_text(D.replace(this.format, "$", this._value)), this.change());
            return a
        },
        set__value: function(a) {
            this._value != a && (this._value = a, this.set_text(D.replace(this.format, "$", this._value)), this.change());
            return a
        },
        __class__: K
    });
    var Za = function() {
        K.call(this);
        this._ivalue = -1;
        this.set_value(0)
    };
    h["dom.HexField"] = Za;
    Za.__name__ = !0;
    Za.__super__ = K;
    Za.prototype = w(K.prototype, {
        get_value: function() {
            return this._ivalue
        },
        set_value: function(a) {
            this._ivalue != a && (this._ivalue = a, this.set__value(D.hex(this._ivalue, 6)));
            return a
        },
        finish: function() {
            var a = B.parseInt("0x" + this._value);
            null == a || null != this.onVerify && !this.onVerify(a) || (this._ivalue = a);
            this.set__value(D.hex(this._ivalue, 6));
            if (null != this.onFinish) this.onFinish(this._ivalue)
        },
        __class__: Za
    });
    var Ob = function() {
        Za.call(this)
    };
    h["dom.ColorField"] = Ob;
    Ob.__name__ = !0;
    Ob.__super__ = Za;
    Ob.prototype = w(Za.prototype, {
        hitTest: function(a, b) {
            return Za.prototype.hitTest.call(this, a - 20, b)
        },
        render: function(a, b) {
            this.m.rect(a + this.x + 2, b + this.y + 2, 16, 16, 3355443);
            this.m.rect(a + this.x + 2, b + this.y + 2, 16, 16, this.get_value());
            Za.prototype.render.call(this, a + 20, b)
        },
        __class__: Ob
    });
    var Nb = function() {
        this.precision = 0;
        K.call(this);
        this._ivalue = -1;
        this.set_value(0)
    };
    h["dom.FloatField"] = Nb;
    Nb.__name__ = !0;
    Nb.__super__ = K;
    Nb.prototype = w(K.prototype, {
        set_value: function(a) {
            this._ivalue !=
                a && (this._ivalue = a, 0 < this.precision ? this._ivalue % 1 >= Math.pow(10, -this.precision) ? this.set__value(this._ivalue.toFixed(this.precision)) : this.set__value("" + (this._ivalue | 0)) : this.set__value("" + this._ivalue));
            return a
        },
        finish: function() {
            var a = parseFloat(this._value);
            isNaN(a) || null != this.onVerify && !this.onVerify(a) || (this._ivalue = a);
            this.set__value("" + this._ivalue);
            if (null != this.onFinish) this.onFinish(this._ivalue)
        },
        __class__: Nb
    });
    var vb = function() {
        K.call(this);
        this._ivalue = -1;
        this.set_value(0)
    };
    h["dom.IntField"] =
        vb;
    vb.__name__ = !0;
    vb.__super__ = K;
    vb.prototype = w(K.prototype, {
        get_value: function() {
            return this._ivalue
        },
        set_value: function(a) {
            this._ivalue != a && (this._ivalue = a, this.set__value("" + this._ivalue));
            return a
        },
        finish: function() {
            var a = B.parseInt(this._value);
            null == a || null != this.onVerify && !this.onVerify(a) || (this._ivalue = a);
            this.set__value("" + this._ivalue);
            if (null != this.onFinish) this.onFinish(this._ivalue)
        },
        __class__: vb
    });
    var wb = function(a, b) {
        this.enabled = !0;
        R.call(this);
        this.anchor = new xb(b, "_blank");
        this.anchor.component.style.zIndex =
            "100";
        this.set_text(a);
        this.block = new yb(this.width, this.height, 0);
        this.anchor.addChild(this.block)
    };
    h["dom.Link"] = wb;
    wb.__name__ = !0;
    wb.__super__ = R;
    wb.prototype = w(R.prototype, {
        set_enabled: function(a) {
            this.enabled != a && ((this.enabled = a) || null == this.anchor.parent || this.anchor.parent.removeChild(this.anchor));
            return a
        },
        render: function(a, b) {
            R.prototype.render.call(this, a, b);
            this.enabled && null == this.anchor.parent && I._main.addChild(this.anchor);
            this.anchor.set_x(a + this.x - this.ofs_x);
            this.anchor.set_y(b +
                this.y - this.ofs_y);
            this.block.set_width(this.width);
            this.block.set_height(this.height)
        },
        __class__: wb
    });
    var W = function() {};
    h["dom.NodeTools"] = W;
    W.__name__ = !0;
    W.setTooltip = function(a, b, c) {
        null == c && (c = 0);
        a.tooltipText = b;
        a.tooltipEnText = b;
        a.tooltipIcon = c;
        return a
    };
    W.setTtIcon = function(a, b) {
        a.tooltipText = "";
        a.tooltipIcon = b;
        return a
    };
    var eb = function() {
        K.call(this)
    };
    h["dom.StringField"] = eb;
    eb.__name__ = !0;
    eb.__super__ = K;
    eb.prototype = w(K.prototype, {
        finish: function() {
            if (null != this.onFinish) this.onFinish(this._value)
        },
        get_value: function() {
            return this._value
        },
        set_value: function(a) {
            return this.set__value(a)
        },
        __class__: eb
    });
    var Ub = function(a) {
        Ea.call(this);
        this.item = a
    };
    h["dom.TinyIcon"] = Ub;
    Ub.__name__ = !0;
    Ub.__super__ = Ea;
    Ub.prototype = w(Ea.prototype, {
        render: function(a, b) {
            this.m.context.drawImage(this.m.imgItems, 40 * (this.item & 31), 40 * (this.item >> 5), 40, 40, a + this.x, b + this.y, 20, 20)
        },
        __class__: Ub
    });
    var tc = function() {};
    h["haxe.IMap"] = tc;
    tc.__name__ = !0;
    var uc = function(a, b) {
        this.high = a;
        this.low = b
    };
    h["haxe._Int64.___Int64"] =
        uc;
    uc.__name__ = !0;
    uc.prototype = {
        __class__: uc
    };
    var Eb = function() {};
    h["haxe.Resource"] = Eb;
    Eb.__name__ = !0;
    Eb.listNames = function() {
        for (var a = [], b = 0, c = Eb.content; b < c.length;) {
            var d = c[b];
            ++b;
            a.push(d.name)
        }
        return a
    };
    var zb = function(a) {
        var b = this;
        this.id = setInterval(function() {
            b.run()
        }, a)
    };
    h["haxe.Timer"] = zb;
    zb.__name__ = !0;
    zb.delay = function(a, b) {
        var c = new zb(b);
        c.run = function() {
            c.stop();
            a()
        };
        return c
    };
    zb.prototype = {
        stop: function() {
            null != this.id && (clearInterval(this.id), this.id = null)
        },
        run: function() {},
        __class__: zb
    };
    var pb = function() {
        this.a1 = 1;
        this.a2 = 0
    };
    h["haxe.crypto.Adler32"] = pb;
    pb.__name__ = !0;
    pb.read = function(a) {
        var b = new pb,
            c = a.readByte(),
            d = a.readByte(),
            e = a.readByte();
        a = a.readByte();
        b.a1 = e << 8 | a;
        b.a2 = c << 8 | d;
        return b
    };
    pb.prototype = {
        update: function(a, b, c) {
            var d = this.a1,
                e = this.a2,
                f = b;
            for (b += c; f < b;) c = f++, d = (d + a.b[c]) % 65521, e = (e + d) % 65521;
            this.a1 = d;
            this.a2 = e
        },
        equals: function(a) {
            return a.a1 == this.a1 && a.a2 == this.a2
        },
        __class__: pb
    };
    var ra = function(a) {
        this.length = a.byteLength;
        this.b = new dc(a);
        this.b.bufferValue = a;
        a.hxBytes =
            this;
        a.bytes = this.b
    };
    h["haxe.io.Bytes"] = ra;
    ra.__name__ = !0;
    ra.alloc = function(a) {
        return new ra(new Ic(a))
    };
    ra.ofData = function(a) {
        var b = a.hxBytes;
        return null != b ? b : new ra(a)
    };
    ra.prototype = {
        blit: function(a, b, c, d) {
            if (0 > a || 0 > c || 0 > d || a + d > this.length || c + d > b.length) throw new z(da.OutsideBounds);
            0 == c && d == b.length ? this.b.set(b.b, a) : this.b.set(b.b.subarray(c, c + d), a)
        },
        getString: function(a, b) {
            if (0 > a || 0 > b || a + b > this.length) throw new z(da.OutsideBounds);
            var c = "",
                d = this.b,
                e = String.fromCharCode,
                f = a;
            for (a += b; f < a;)
                if (b =
                    d[f++], 128 > b) {
                    if (0 == b) break;
                    c += e(b)
                } else if (224 > b) c += e((b & 63) << 6 | d[f++] & 127);
            else if (240 > b) {
                var g = d[f++];
                c += e((b & 31) << 12 | (g & 127) << 6 | d[f++] & 127)
            } else {
                g = d[f++];
                var m = d[f++];
                b = (b & 15) << 18 | (g & 127) << 12 | (m & 127) << 6 | d[f++] & 127;
                c += e((b >> 10) + 55232);
                c += e(b & 1023 | 56320)
            }
            return c
        },
        toString: function() {
            return this.getString(0, this.length)
        },
        __class__: ra
    };
    var Ba = function() {
        this.h = {}
    };
    h["haxe.ds.IntMap"] = Ba;
    Ba.__name__ = !0;
    Ba.__interfaces__ = [tc];
    Ba.prototype = {
        __class__: Ba
    };
    var ib = function() {
        this.h = {};
        this.h.__keys__ = {}
    };
    h["haxe.ds.ObjectMap"] = ib;
    ib.__name__ = !0;
    ib.__interfaces__ = [tc];
    ib.prototype = {
        set: function(a, b) {
            var c = a.__id__ || (a.__id__ = ++ib.count);
            this.h[c] = b;
            this.h.__keys__[c] = a
        },
        remove: function(a) {
            a = a.__id__;
            if (null == this.h.__keys__[a]) return !1;
            delete this.h[a];
            delete this.h.__keys__[a];
            return !0
        },
        __class__: ib
    };
    var Y = function() {
        this.h = {}
    };
    h["haxe.ds.StringMap"] = Y;
    Y.__name__ = !0;
    Y.__interfaces__ = [tc];
    Y.prototype = {
        set: function(a, b) {
            null != Jc[a] ? this.setReserved(a, b) : this.h[a] = b
        },
        get: function(a) {
            return null !=
                Jc[a] ? this.getReserved(a) : this.h[a]
        },
        exists: function(a) {
            return null != Jc[a] ? this.existsReserved(a) : this.h.hasOwnProperty(a)
        },
        setReserved: function(a, b) {
            null == this.rh && (this.rh = {});
            this.rh["$" + a] = b
        },
        getReserved: function(a) {
            return null == this.rh ? null : this.rh["$" + a]
        },
        existsReserved: function(a) {
            return null == this.rh ? !1 : this.rh.hasOwnProperty("$" + a)
        },
        remove: function(a) {
            if (null != Jc[a]) {
                a = "$" + a;
                if (null == this.rh || !this.rh.hasOwnProperty(a)) return !1;
                delete this.rh[a]
            } else {
                if (!this.h.hasOwnProperty(a)) return !1;
                delete this.h[a]
            }
            return !0
        },
        keys: function() {
            var a = this.arrayKeys();
            return y.iter(a)
        },
        arrayKeys: function() {
            var a = [],
                b;
            for (b in this.h) this.h.hasOwnProperty(b) && a.push(b);
            if (null != this.rh)
                for (b in this.rh) 36 == b.charCodeAt(0) && a.push(b.substr(1));
            return a
        },
        __class__: Y
    };
    var ec = function() {
        this.b = []
    };
    h["haxe.io.BytesBuffer"] = ec;
    ec.__name__ = !0;
    ec.prototype = {
        add: function(a) {
            var b = a.b,
                c = 0;
            for (a = a.length; c < a;) {
                var d = c++;
                this.b.push(b[d])
            }
        },
        addBytes: function(a, b, c) {
            if (0 > b || 0 > c || b + c > a.length) throw new z(da.OutsideBounds);
            a = a.b;
            var d = b;
            for (b += c; d < b;) c = d++, this.b.push(a[c])
        },
        getBytes: function() {
            var a = new ra((new dc(this.b)).buffer);
            this.b = null;
            return a
        },
        __class__: ec
    };
    var fc = function() {};
    h["haxe.io.Input"] = fc;
    fc.__name__ = !0;
    fc.prototype = {
        readByte: function() {
            throw new z("Not implemented");
        },
        readBytes: function(a, b, c) {
            var d = c,
                e = a.b;
            if (0 > b || 0 > c || b + c > a.length) throw new z(da.OutsideBounds);
            for (; 0 < d;) e[b] = this.readByte(), b++, d--;
            return c
        },
        readFullBytes: function(a, b, c) {
            for (; 0 < c;) {
                var d = this.readBytes(a, b, c);
                b += d;
                c -= d
            }
        },
        read: function(a) {
            for (var b =
                    ra.alloc(a), c = 0; 0 < a;) {
                var d = this.readBytes(b, c, a);
                if (0 == d) throw new z(da.Blocked);
                c += d;
                a -= d
            }
            return b
        },
        readInt16: function() {
            var a = this.readByte(),
                b = this.readByte();
            a = this.bigEndian ? b | a << 8 : a | b << 8;
            return 0 != (a & 32768) ? a - 65536 : a
        },
        readUInt16: function() {
            var a = this.readByte(),
                b = this.readByte();
            return this.bigEndian ? b | a << 8 : a | b << 8
        },
        readInt32: function() {
            var a = this.readByte(),
                b = this.readByte(),
                c = this.readByte(),
                d = this.readByte();
            return this.bigEndian ? d | c << 8 | b << 16 | a << 24 : a | b << 8 | c << 16 | d << 24
        },
        readString: function(a) {
            var b =
                ra.alloc(a);
            this.readFullBytes(b, 0, a);
            return b.toString()
        },
        __class__: fc
    };
    var gc = function(a, b, c) {
        null == b && (b = 0);
        null == c && (c = a.length - b);
        if (0 > b || 0 > c || b + c > a.length) throw new z(da.OutsideBounds);
        this.b = a.b;
        this.pos = b;
        this.totlen = this.len = c
    };
    h["haxe.io.BytesInput"] = gc;
    gc.__name__ = !0;
    gc.__super__ = fc;
    gc.prototype = w(fc.prototype, {
        readByte: function() {
            if (0 == this.len) throw new z(new hc);
            this.len--;
            return this.b[this.pos++]
        },
        readBytes: function(a, b, c) {
            if (0 > b || 0 > c || b + c > a.length) throw new z(da.OutsideBounds);
            if (0 ==
                this.len && 0 < c) throw new z(new hc);
            this.len < c && (c = this.len);
            var d = this.b;
            a = a.b;
            for (var e = 0; e < c;) {
                var f = e++;
                a[b + f] = d[this.pos + f]
            }
            this.pos += c;
            this.len -= c;
            return c
        },
        __class__: gc
    });
    var hc = function() {};
    h["haxe.io.Eof"] = hc;
    hc.__name__ = !0;
    hc.prototype = {
        toString: function() {
            return "Eof"
        },
        __class__: hc
    };
    var da = h["haxe.io.Error"] = {
        __ename__: !0,
        __constructs__: ["Blocked", "Overflow", "OutsideBounds", "Custom"]
    };
    da.Blocked = ["Blocked", 0];
    da.Blocked.toString = v;
    da.Blocked.__enum__ = da;
    da.Overflow = ["Overflow", 1];
    da.Overflow.toString =
        v;
    da.Overflow.__enum__ = da;
    da.OutsideBounds = ["OutsideBounds", 2];
    da.OutsideBounds.toString = v;
    da.OutsideBounds.__enum__ = da;
    da.Custom = function(a) {
        a = ["Custom", 3, a];
        a.__enum__ = da;
        a.toString = v;
        return a
    };
    var Ma = function() {};
    h["haxe.io.FPHelper"] = Ma;
    Ma.__name__ = !0;
    Ma.i32ToFloat = function(a) {
        var b = a >>> 23 & 255,
            c = a & 8388607;
        return 0 == c && 0 == b ? 0 : (1 - (a >>> 31 << 1)) * (1 + Math.pow(2, -23) * c) * Math.pow(2, b - 127)
    };
    Ma.floatToI32 = function(a) {
        if (0 == a) return 0;
        var b = 0 > a ? -a : a;
        var c = Math.floor(Math.log(b) / .6931471805599453); - 127 > c ?
            c = -127 : 128 < c && (c = 128);
        return (0 > a ? -2147483648 : 0) | c + 127 << 23 | Math.round(8388608 * (b / Math.pow(2, c) - 1)) & 8388607
    };
    Ma.i64ToDouble = function(a, b) {
        var c = (b >> 20 & 2047) - 1023;
        a = 4294967296 * (b & 1048575) + 2147483648 * (a >>> 31) + (a & 2147483647);
        return 0 == a && -1023 == c ? 0 : (1 - (b >>> 31 << 1)) * (1 + Math.pow(2, -52) * a) * Math.pow(2, c)
    };
    Ma.doubleToI64 = function(a) {
        var b = Ma.i64tmp;
        if (0 == a) b.low = 0, b.high = 0;
        else {
            var c = 0 > a ? -a : a;
            var d = Math.floor(Math.log(c) / .6931471805599453);
            c = Math.round(4503599627370496 * (c / Math.pow(2, d) - 1));
            b.low = c | 0;
            b.high =
                (0 > a ? -2147483648 : 0) | d + 1023 << 20 | c / 4294967296 | 0
        }
        return b
    };
    var Na = h["haxe.zip.ExtraField"] = {
        __ename__: !0,
        __constructs__: ["FUnknown", "FInfoZipUnicodePath", "FUtf8"]
    };
    Na.FUnknown = function(a, b) {
        a = ["FUnknown", 0, a, b];
        a.__enum__ = Na;
        a.toString = v;
        return a
    };
    Na.FInfoZipUnicodePath = function(a, b) {
        a = ["FInfoZipUnicodePath", 1, a, b];
        a.__enum__ = Na;
        a.toString = v;
        return a
    };
    Na.FUtf8 = ["FUtf8", 2];
    Na.FUtf8.toString = v;
    Na.FUtf8.__enum__ = Na;
    var Oa = h["haxe.zip.Huffman"] = {
        __ename__: !0,
        __constructs__: ["Found", "NeedBit", "NeedBits"]
    };
    Oa.Found = function(a) {
        a = ["Found", 0, a];
        a.__enum__ = Oa;
        a.toString = v;
        return a
    };
    Oa.NeedBit = function(a, b) {
        a = ["NeedBit", 1, a, b];
        a.__enum__ = Oa;
        a.toString = v;
        return a
    };
    Oa.NeedBits = function(a, b) {
        a = ["NeedBits", 2, a, b];
        a.__enum__ = Oa;
        a.toString = v;
        return a
    };
    var vc = function() {};
    h["haxe.zip.HuffTools"] = vc;
    vc.__name__ = !0;
    vc.prototype = {
        treeDepth: function(a) {
            switch (a[1]) {
                case 0:
                    return 0;
                case 2:
                    throw new z("assert");
                case 1:
                    var b = a[3];
                    a = this.treeDepth(a[2]);
                    b = this.treeDepth(b);
                    return 1 + (a < b ? a : b)
            }
        },
        treeCompress: function(a) {
            var b =
                this.treeDepth(a);
            if (0 == b) return a;
            if (1 == b) switch (a[1]) {
                case 1:
                    return b = a[3], Oa.NeedBit(this.treeCompress(a[2]), this.treeCompress(b));
                default:
                    throw new z("assert");
            }
            for (var c = 1 << b, d = [], e = 0; e < c;) e++, d.push(Oa.Found(-1));
            this.treeWalk(d, 0, 0, b, a);
            return Oa.NeedBits(b, d)
        },
        treeWalk: function(a, b, c, d, e) {
            switch (e[1]) {
                case 1:
                    var f = e[3],
                        g = e[2];
                    0 < d ? (this.treeWalk(a, b, c + 1, d - 1, g), this.treeWalk(a, b | 1 << c, c + 1, d - 1, f)) : a[b] = this.treeCompress(e);
                    break;
                default:
                    a[b] = this.treeCompress(e)
            }
        },
        treeMake: function(a, b, c, d) {
            if (d >
                b) throw new z("Invalid huffman");
            var e = c << 5 | d;
            if (a.h.hasOwnProperty(e)) return Oa.Found(a.h[e]);
            c <<= 1;
            d += 1;
            return Oa.NeedBit(this.treeMake(a, b, c, d), this.treeMake(a, b, c | 1, d))
        },
        make: function(a, b, c, d) {
            var e = [],
                f = [];
            if (32 < d) throw new z("Invalid huffman");
            for (var g = 0; g < d;) g++, e.push(0), f.push(0);
            for (g = 0; g < c;) {
                var m = g++;
                m = a[m + b];
                if (m >= d) throw new z("Invalid huffman");
                e[m]++
            }
            g = 0;
            m = 1;
            for (var h = d - 1; m < h;) {
                var k = m++;
                g = g + e[k] << 1;
                f[k] = g
            }
            e = new Ba;
            for (g = 0; g < c;) m = g++, h = a[m + b], 0 != h && (k = f[h - 1], f[h - 1] = k + 1, e.h[k << 5 | h] =
                m);
            return this.treeCompress(Oa.NeedBit(this.treeMake(e, d, 0, 1), this.treeMake(e, d, 1, 1)))
        },
        __class__: vc
    };
    var wc = function(a) {
        this.buffer = ra.alloc(65536);
        this.pos = 0;
        a && (this.crc = new pb)
    };
    h["haxe.zip._InflateImpl.Window"] = wc;
    wc.__name__ = !0;
    wc.prototype = {
        slide: function() {
            null != this.crc && this.crc.update(this.buffer, 0, 32768);
            var a = ra.alloc(65536);
            this.pos -= 32768;
            a.blit(0, this.buffer, 32768, this.pos);
            this.buffer = a
        },
        addBytes: function(a, b, c) {
            65536 < this.pos + c && this.slide();
            this.buffer.blit(this.pos, a, b, c);
            this.pos +=
                c
        },
        addByte: function(a) {
            65536 == this.pos && this.slide();
            this.buffer.b[this.pos] = a & 255;
            this.pos++
        },
        getLastChar: function() {
            return this.buffer.b[this.pos - 1]
        },
        available: function() {
            return this.pos
        },
        checksum: function() {
            null != this.crc && this.crc.update(this.buffer, 0, this.pos);
            return this.crc
        },
        __class__: wc
    };
    var E = h["haxe.zip._InflateImpl.State"] = {
        __ename__: !0,
        __constructs__: "Head Block CData Flat Crc Dist DistOne Done".split(" ")
    };
    E.Head = ["Head", 0];
    E.Head.toString = v;
    E.Head.__enum__ = E;
    E.Block = ["Block", 1];
    E.Block.toString =
        v;
    E.Block.__enum__ = E;
    E.CData = ["CData", 2];
    E.CData.toString = v;
    E.CData.__enum__ = E;
    E.Flat = ["Flat", 3];
    E.Flat.toString = v;
    E.Flat.__enum__ = E;
    E.Crc = ["Crc", 4];
    E.Crc.toString = v;
    E.Crc.__enum__ = E;
    E.Dist = ["Dist", 5];
    E.Dist.toString = v;
    E.Dist.__enum__ = E;
    E.DistOne = ["DistOne", 6];
    E.DistOne.toString = v;
    E.DistOne.__enum__ = E;
    E.Done = ["Done", 7];
    E.Done.toString = v;
    E.Done.__enum__ = E;
    var ka = function(a, b, c) {
        null == c && (c = !0);
        null == b && (b = !0);
        this["final"] = !1;
        this.htools = new vc;
        this.huffman = this.buildFixedHuffman();
        this.huffdist = null;
        this.dist = this.len = 0;
        this.state = b ? E.Head : E.Block;
        this.input = a;
        this.needed = this.nbits = this.bits = 0;
        this.output = null;
        this.outpos = 0;
        this.lengths = [];
        for (a = 0; 19 > a;) a++, this.lengths.push(-1);
        this.window = new wc(c)
    };
    h["haxe.zip.InflateImpl"] = ka;
    ka.__name__ = !0;
    ka.prototype = {
        buildFixedHuffman: function() {
            if (null != ka.FIXED_HUFFMAN) return ka.FIXED_HUFFMAN;
            for (var a = [], b = 0; 288 > b;) {
                var c = b++;
                a.push(143 >= c ? 8 : 255 >= c ? 9 : 279 >= c ? 7 : 8)
            }
            ka.FIXED_HUFFMAN = this.htools.make(a, 0, 288, 10);
            return ka.FIXED_HUFFMAN
        },
        readBytes: function(a,
            b, c) {
            this.needed = c;
            this.outpos = b;
            this.output = a;
            if (0 < c)
                for (; this.inflateLoop(););
            return c - this.needed
        },
        getBits: function(a) {
            for (; this.nbits < a;) this.bits |= this.input.readByte() << this.nbits, this.nbits += 8;
            var b = this.bits & (1 << a) - 1;
            this.nbits -= a;
            this.bits >>= a;
            return b
        },
        getBit: function() {
            0 == this.nbits && (this.nbits = 8, this.bits = this.input.readByte());
            var a = 1 == (this.bits & 1);
            this.nbits--;
            this.bits >>= 1;
            return a
        },
        getRevBits: function(a) {
            return 0 == a ? 0 : this.getBit() ? 1 << a - 1 | this.getRevBits(a - 1) : this.getRevBits(a - 1)
        },
        resetBits: function() {
            this.nbits = this.bits = 0
        },
        addBytes: function(a, b, c) {
            this.window.addBytes(a, b, c);
            this.output.blit(this.outpos, a, b, c);
            this.needed -= c;
            this.outpos += c
        },
        addByte: function(a) {
            this.window.addByte(a);
            this.output.b[this.outpos] = a & 255;
            this.needed--;
            this.outpos++
        },
        addDistOne: function(a) {
            for (var b = this.window.getLastChar(), c = 0; c < a;) c++, this.addByte(b)
        },
        addDist: function(a, b) {
            this.addBytes(this.window.buffer, this.window.pos - a, b)
        },
        applyHuffman: function(a) {
            switch (a[1]) {
                case 0:
                    return a[2];
                case 1:
                    var b =
                        a[3];
                    a = a[2];
                    return this.applyHuffman(this.getBit() ? b : a);
                case 2:
                    return this.applyHuffman(a[3][this.getBits(a[2])])
            }
        },
        inflateLengths: function(a, b) {
            for (var c = 0, d = 0; c < b;) {
                var e = this.applyHuffman(this.huffman);
                switch (e) {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                        d = e;
                        a[c] = e;
                        c++;
                        break;
                    case 16:
                        e = c + 3 + this.getBits(2);
                        if (e > b) throw new z("Invalid data");
                        for (; c < e;) a[c] = d, c++;
                        break;
                    case 17:
                        c += 3 + this.getBits(3);
                        if (c > b) throw new z("Invalid data");
                        break;
                    case 18:
                        c += 11 + this.getBits(7);
                        if (c > b) throw new z("Invalid data");
                        break;
                    default:
                        throw new z("Invalid data");
                }
            }
        },
        inflateLoop: function() {
            switch (this.state[1]) {
                case 0:
                    var a = this.input.readByte();
                    if (8 != (a & 15)) throw new z("Invalid data");
                    var b = this.input.readByte(),
                        c = 0 != (b & 32);
                    if (0 != ((a << 8) + b) % 31) throw new z("Invalid data");
                    if (c) throw new z("Unsupported dictionary");
                    this.state = E.Block;
                    return !0;
                case 4:
                    a = this.window.checksum();
                    if (null == a) return this.state = E.Done, !0;
                    b = pb.read(this.input);
                    if (!a.equals(b)) throw new z("Invalid CRC");
                    this.state = E.Done;
                    return !0;
                case 7:
                    return !1;
                case 1:
                    switch (this["final"] = this.getBit(), this.getBits(2)) {
                        case 0:
                            this.len = this.input.readUInt16();
                            if (this.input.readUInt16() != 65535 - this.len) throw new z("Invalid data");
                            this.state = E.Flat;
                            a = this.inflateLoop();
                            this.resetBits();
                            return a;
                        case 1:
                            return this.huffman = this.buildFixedHuffman(), this.huffdist = null, this.state = E.CData, !0;
                        case 2:
                            a = this.getBits(5) + 257;
                            b = this.getBits(5) + 1;
                            c = this.getBits(4) + 4;
                            for (var d = 0; d < c;) {
                                var e = d++;
                                this.lengths[ka.CODE_LENGTHS_POS[e]] =
                                    this.getBits(3)
                            }
                            for (; 19 > c;) d = c++, this.lengths[ka.CODE_LENGTHS_POS[d]] = 0;
                            this.huffman = this.htools.make(this.lengths, 0, 19, 8);
                            c = [];
                            d = 0;
                            for (e = a + b; d < e;) d++, c.push(0);
                            this.inflateLengths(c, a + b);
                            this.huffdist = this.htools.make(c, a, b, 16);
                            this.huffman = this.htools.make(c, 0, a, 16);
                            this.state = E.CData;
                            return !0;
                        default:
                            throw new z("Invalid data");
                    }
                case 3:
                    return a = this.len < this.needed ? this.len : this.needed, b = this.input.read(a), this.len -= a, this.addBytes(b, 0, a), 0 == this.len && (this.state = this["final"] ? E.Crc : E.Block), 0 <
                        this.needed;
                case 6:
                    return a = this.len < this.needed ? this.len : this.needed, this.addDistOne(a), this.len -= a, 0 == this.len && (this.state = E.CData), 0 < this.needed;
                case 5:
                    for (; 0 < this.len && 0 < this.needed;) a = this.len < this.dist ? this.len : this.dist, a = this.needed < a ? this.needed : a, this.addDist(this.dist, a), this.len -= a;
                    0 == this.len && (this.state = E.CData);
                    return 0 < this.needed;
                case 2:
                    b = this.applyHuffman(this.huffman);
                    if (256 > b) return this.addByte(b), 0 < this.needed;
                    if (256 == b) this.state = this["final"] ? E.Crc : E.Block;
                    else {
                        b -= 257;
                        a =
                            ka.LEN_EXTRA_BITS_TBL[b];
                        if (-1 == a) throw new z("Invalid data");
                        this.len = ka.LEN_BASE_VAL_TBL[b] + this.getBits(a);
                        b = null == this.huffdist ? this.getRevBits(5) : this.applyHuffman(this.huffdist);
                        a = ka.DIST_EXTRA_BITS_TBL[b];
                        if (-1 == a) throw new z("Invalid data");
                        this.dist = ka.DIST_BASE_VAL_TBL[b] + this.getBits(a);
                        if (this.dist > this.window.available()) throw new z("Invalid data");
                        this.state = 1 == this.dist ? E.DistOne : E.Dist
                    }
                    return !0
            }
        },
        __class__: ka
    };
    var Bb = function(a) {
        this.i = a
    };
    h["haxe.zip.Reader"] = Bb;
    Bb.__name__ = !0;
    Bb.readZip =
        function(a) {
            return (new Bb(a)).read()
        };
    Bb.prototype = {
        readZipDate: function() {
            var a = this.i.readUInt16(),
                b = a >> 11 & 31,
                c = a >> 5 & 63;
            a &= 31;
            var d = this.i.readUInt16();
            return new Date((d >> 9) + 1980, (d >> 5 & 15) - 1, d & 31, b, c, a << 1)
        },
        readExtraFields: function(a) {
            for (var b = new Lb; 0 < a;) {
                if (4 > a) throw new z("Invalid extra fields data");
                var c = this.i.readUInt16(),
                    d = this.i.readUInt16();
                if (a < d) throw new z("Invalid extra fields data");
                switch (c) {
                    case 28789:
                        var e = this.i.readByte();
                        if (1 != e) {
                            var f = new ec;
                            f.b.push(e);
                            f.add(this.i.read(d -
                                1));
                            b.add(Na.FUnknown(c, f.getBytes()))
                        } else c = this.i.readInt32(), e = this.i.read(d - 5).toString(), b.add(Na.FInfoZipUnicodePath(e, c));
                        break;
                    default:
                        b.add(Na.FUnknown(c, this.i.read(d)))
                }
                a -= 4 + d
            }
            return b
        },
        readEntryHeader: function() {
            var a = this.i,
                b = a.readInt32();
            if (33639248 == b || 101010256 == b) return null;
            if (67324752 != b) throw new z("Invalid Zip Data");
            a.readUInt16();
            b = a.readUInt16();
            var c = 0 != (b & 2048);
            if (0 != (b & 63473)) throw new z("Unsupported flags " + b);
            var d = a.readUInt16(),
                e = 0 != d;
            if (e && 8 != d) throw new z("Unsupported compression " +
                d);
            d = this.readZipDate();
            var f = a.readInt32(),
                g = a.readInt32(),
                m = a.readInt32(),
                h = a.readInt16(),
                k = a.readInt16();
            a = a.readString(h);
            k = this.readExtraFields(k);
            c && k.push(Na.FUtf8);
            0 != (b & 8) && (f = null);
            return {
                fileName: a,
                fileSize: m,
                fileTime: d,
                compressed: e,
                dataSize: g,
                data: null,
                crc32: f,
                extraFields: k
            }
        },
        read: function() {
            for (var a = new Lb, b = null;;) {
                var c = this.readEntryHeader();
                if (null == c) break;
                if (null == c.crc32) {
                    if (c.compressed) {
                        null == b && (b = ra.alloc(65536));
                        for (var d = new ec, e = new ka(this.i, !1, !1);;) {
                            var f = e.readBytes(b,
                                0, 65536);
                            d.addBytes(b, 0, f);
                            if (65536 > f) break
                        }
                        c.data = d.getBytes()
                    } else c.data = this.i.read(c.dataSize);
                    c.crc32 = this.i.readInt32();
                    134695760 == c.crc32 && (c.crc32 = this.i.readInt32());
                    c.dataSize = this.i.readInt32();
                    c.fileSize = this.i.readInt32();
                    c.dataSize = c.fileSize;
                    c.compressed = !1
                } else c.data = this.i.read(c.dataSize);
                a.add(c)
            }
            return a
        },
        __class__: Bb
    };
    var z = function(a) {
        Error.call(this);
        this.val = a;
        Object.prototype.hasOwnProperty.call(a, "name") ? this.name = oa.field(a, "name") : this.name = "Error";
        Object.prototype.hasOwnProperty.call(a,
            "message") ? this.message = oa.field(a, "message") : this.message = B.string(a);
        Error.captureStackTrace && Error.captureStackTrace(this, z)
    };
    h["js._Boot.HaxeError"] = z;
    z.__name__ = !0;
    z.__super__ = Error;
    z.prototype = w(Error.prototype, {
        __class__: z
    });
    var J = function() {};
    h["js.Boot"] = J;
    J.__name__ = !0;
    J.getClass = function(a) {
        if (a instanceof Array && null == a.__enum__) return Array;
        var b = a.__class__;
        if (null != b) return b;
        a = J.__nativeClassName(a);
        return null != a ? J.__resolveNativeClass(a) : null
    };
    J.__string_rec = function(a, b) {
        if (null ==
            a) return "null";
        if (5 <= b.length) return "<...>";
        var c = typeof a;
        "function" == c && (a.__name__ || a.__ename__) && (c = "object");
        switch (c) {
            case "object":
                if (a instanceof Array) {
                    if (a.__enum__) {
                        if (2 == a.length) return a[0];
                        c = a[0] + "(";
                        b += "\t";
                        for (var d = 2, e = a.length; d < e;) {
                            var f = d++;
                            c = 2 != f ? c + ("," + J.__string_rec(a[f], b)) : c + J.__string_rec(a[f], b)
                        }
                        return c + ")"
                    }
                    c = a.length;
                    d = "[";
                    b += "\t";
                    for (e = 0; e < c;) f = e++, d += (0 < f ? "," : "") + J.__string_rec(a[f], b);
                    return d + "]"
                }
                try {
                    d = a.toString
                } catch (g) {
                    return g instanceof z && (g = g.val), "???"
                }
                if (null !=
                    d && d != Object.toString && "function" == typeof d && (c = a.toString(), "[object Object]" != c)) return c;
                c = null;
                d = "{\n";
                b += "\t";
                e = null != a.hasOwnProperty;
                for (c in a) e && !a.hasOwnProperty(c) || "prototype" == c || "__class__" == c || "__super__" == c || "__interfaces__" == c || "__properties__" == c || (2 != d.length && (d += ", \n"), d += b + c + " : " + J.__string_rec(a[c], b));
                b = b.substring(1);
                return d + ("\n" + b + "}");
            case "function":
                return "<function>";
            case "string":
                return a;
            default:
                return String(a)
        }
    };
    J.__interfLoop = function(a, b) {
        if (null == a) return !1;
        if (a == b) return !0;
        var c = a.__interfaces__;
        if (null != c)
            for (var d = 0, e = c.length; d < e;) {
                var f = d++;
                f = c[f];
                if (f == b || J.__interfLoop(f, b)) return !0
            }
        return J.__interfLoop(a.__super__, b)
    };
    J.__instanceof = function(a, b) {
        if (null == b) return !1;
        switch (b) {
            case ad:
                return (a | 0) === a;
            case Tc:
                return "number" == typeof a;
            case Uc:
                return "boolean" == typeof a;
            case String:
                return "string" == typeof a;
            case Array:
                return a instanceof Array && null == a.__enum__;
            case bd:
                return !0;
            default:
                if (null != a)
                    if ("function" == typeof b) {
                        if (a instanceof b || J.__interfLoop(J.getClass(a),
                                b)) return !0
                    } else {
                        if ("object" == typeof b && J.__isNativeObj(b) && a instanceof b) return !0
                    }
                else return !1;
                return b == cd && null != a.__name__ || b == dd && null != a.__ename__ ? !0 : a.__enum__ == b
        }
    };
    J.__nativeClassName = function(a) {
        a = J.__toStr.call(a).slice(8, -1);
        return "Object" == a || "Function" == a || "Math" == a || "JSON" == a ? null : a
    };
    J.__isNativeObj = function(a) {
        return null != J.__nativeClassName(a)
    };
    J.__resolveNativeClass = function(a) {
        return Function("return typeof " + a + ' != "undefined" ? ' + a + " : null")()
    };
    var Pa = function(a) {
        if (a instanceof Array && null == a.__enum__) this.a = a, this.byteLength = a.length;
        else {
            this.a = [];
            for (var b = 0; b < a;) {
                var c = b++;
                this.a[c] = 0
            }
            this.byteLength = a
        }
    };
    h["js.html.compat.ArrayBuffer"] = Pa;
    Pa.__name__ = !0;
    Pa.sliceImpl = function(a, b) {
        a = new dc(this, a, null == b ? null : b - a);
        b = new Ic(a.byteLength);
        (new dc(b)).set(a);
        return b
    };
    Pa.prototype = {
        slice: function(a, b) {
            return new Pa(this.a.slice(a, b))
        },
        __class__: Pa
    };
    var xc = function(a, b, c) {
        this.buf = a;
        this.offset = null == b ? 0 : b;
        this.length = null == c ? a.byteLength - this.offset : c;
        if (0 > this.offset ||
            0 > this.length || this.offset + this.length > a.byteLength) throw new z(da.OutsideBounds);
    };
    h["js.html.compat.DataView"] = xc;
    xc.__name__ = !0;
    xc.prototype = {
        getInt8: function(a) {
            a = this.buf.a[this.offset + a];
            return 128 <= a ? a - 256 : a
        },
        getUint8: function(a) {
            return this.buf.a[this.offset + a]
        },
        getInt16: function(a, b) {
            a = this.getUint16(a, b);
            return 32768 <= a ? a - 65536 : a
        },
        getUint16: function(a, b) {
            return b ? this.buf.a[this.offset + a] | this.buf.a[this.offset + a + 1] << 8 : this.buf.a[this.offset + a] << 8 | this.buf.a[this.offset + a + 1]
        },
        getInt32: function(a,
            b) {
            var c = this.offset + a;
            a = this.buf.a[c++];
            var d = this.buf.a[c++],
                e = this.buf.a[c++];
            c = this.buf.a[c++];
            return b ? a | d << 8 | e << 16 | c << 24 : c | e << 8 | d << 16 | a << 24
        },
        getUint32: function(a, b) {
            a = this.getInt32(a, b);
            return 0 > a ? a + 4294967296 : a
        },
        getFloat32: function(a, b) {
            return Ma.i32ToFloat(this.getInt32(a, b))
        },
        getFloat64: function(a, b) {
            var c = this.getInt32(a, b);
            a = this.getInt32(a + 4, b);
            return Ma.i64ToDouble(b ? c : a, b ? a : c)
        },
        setInt8: function(a, b) {
            this.buf.a[a + this.offset] = 0 > b ? b + 128 & 255 : b & 255
        },
        setUint8: function(a, b) {
            this.buf.a[a + this.offset] =
                b & 255
        },
        setInt16: function(a, b, c) {
            this.setUint16(a, 0 > b ? b + 65536 : b, c)
        },
        setUint16: function(a, b, c) {
            a += this.offset;
            c ? (this.buf.a[a] = b & 255, this.buf.a[a++] = b >> 8 & 255) : (this.buf.a[a++] = b >> 8 & 255, this.buf.a[a] = b & 255)
        },
        setInt32: function(a, b, c) {
            this.setUint32(a, b, c)
        },
        setUint32: function(a, b, c) {
            a += this.offset;
            c ? (this.buf.a[a++] = b & 255, this.buf.a[a++] = b >> 8 & 255, this.buf.a[a++] = b >> 16 & 255, this.buf.a[a++] = b >>> 24) : (this.buf.a[a++] = b >>> 24, this.buf.a[a++] = b >> 16 & 255, this.buf.a[a++] = b >> 8 & 255, this.buf.a[a++] = b & 255)
        },
        setFloat32: function(a,
            b, c) {
            this.setUint32(a, Ma.floatToI32(b), c)
        },
        setFloat64: function(a, b, c) {
            b = Ma.doubleToI64(b);
            c ? (this.setUint32(a, b.low), this.setUint32(a, b.high)) : (this.setUint32(a, b.high), this.setUint32(a, b.low))
        },
        __class__: xc
    };
    var $a = function() {};
    h["js.html.compat.Uint8Array"] = $a;
    $a.__name__ = !0;
    $a._new = function(a, b, c) {
        if ("number" == typeof a) {
            c = [];
            for (b = 0; b < a;) {
                var d = b++;
                c[d] = 0
            }
            c.byteLength = c.length;
            c.byteOffset = 0;
            c.buffer = new Pa(c)
        } else if (J.__instanceof(a, Pa)) null == b && (b = 0), null == c && (c = a.byteLength - b), c = 0 == b ? a.a : a.a.slice(b,
            b + c), c.byteLength = c.length, c.byteOffset = b, c.buffer = a;
        else if (a instanceof Array && null == a.__enum__) c = a.slice(), c.byteLength = c.length, c.byteOffset = 0, c.buffer = new Pa(c);
        else throw new z("TODO " + B.string(a));
        c.subarray = $a._subarray;
        c.set = $a._set;
        return c
    };
    $a._set = function(a, b) {
        if (J.__instanceof(a.buffer, Pa)) {
            if (a.byteLength + b > this.byteLength) throw new z("set() outside of range");
            for (var c = 0, d = a.byteLength; c < d;) {
                var e = c++;
                this[e + b] = a[e]
            }
        } else if (a instanceof Array && null == a.__enum__) {
            if (a.length + b > this.byteLength) throw new z("set() outside of range");
            c = 0;
            for (d = a.length; c < d;) e = c++, this[e + b] = a[e]
        } else throw new z("TODO");
    };
    $a._subarray = function(a, b) {
        b = $a._new(this.slice(a, b));
        b.byteOffset = a;
        return b
    };
    var yc = function() {
        this.__enabled = !0;
        this.bitmapData = new Y;
        this.font = new Y;
        this.sound = new Y
    };
    h["openfl.AssetCache"] = yc;
    yc.__name__ = !0;
    yc.prototype = {
        get_enabled: function() {
            return this.__enabled
        },
        __class__: yc
    };
    var Q = function() {};
    h["openfl.Assets"] = Q;
    Q.__name__ = !0;
    Q.getBitmapData = function(a, b) {
        null == b && (b = !0);
        Q.initialize();
        var c = null,
            d, e;
        if (b && (d = Q.cache).get_enabled() &&
            d.bitmapData.exists(a) && Q.isValidBitmapData(e = Q.cache.bitmapData.get(a))) return e;
        var f = a.indexOf(":");
        e = a.substring(0, f);
        f = a.substring(f + 1);
        e = Q.getLibrary(e);
        null != e ? e.exists(f, G.IMAGE) ? (c = e.getBitmapData(f), b ? d.get_enabled() && d.bitmapData.set(a, c) : c = c.clone()) : null : null;
        return c
    };
    Q.getLibrary = function(a) {
        return Q.libraries.get(null == a || "" == a ? "default" : a)
    };
    Q.initialize = function() {
        Q.initialized || (Q.registerLibrary("default", new Kb), Q.initialized = !0)
    };
    Q.isValidBitmapData = function(a) {
        return !0
    };
    Q.registerLibrary =
        function(a, b) {
            Q.libraries.exists(a) && Q.unloadLibrary(a);
            Q.libraries.set(a, b)
        };
    Q.unloadLibrary = function(a) {
        Q.initialize();
        for (var b = Q.cache.bitmapData.keys(); b.hasNext();) {
            var c = b.next();
            c.substring(0, c.indexOf(":")) == a && Q.cache.bitmapData.remove(c)
        }
        Q.libraries.remove(a)
    };
    var G = h["openfl.AssetType"] = {
        __ename__: !0,
        __constructs__: "BINARY FONT IMAGE MOVIE_CLIP MUSIC SOUND TEMPLATE TEXT".split(" ")
    };
    G.BINARY = ["BINARY", 0];
    G.BINARY.toString = v;
    G.BINARY.__enum__ = G;
    G.FONT = ["FONT", 1];
    G.FONT.toString = v;
    G.FONT.__enum__ =
        G;
    G.IMAGE = ["IMAGE", 2];
    G.IMAGE.toString = v;
    G.IMAGE.__enum__ = G;
    G.MOVIE_CLIP = ["MOVIE_CLIP", 3];
    G.MOVIE_CLIP.toString = v;
    G.MOVIE_CLIP.__enum__ = G;
    G.MUSIC = ["MUSIC", 4];
    G.MUSIC.toString = v;
    G.MUSIC.__enum__ = G;
    G.SOUND = ["SOUND", 5];
    G.SOUND.toString = v;
    G.SOUND.__enum__ = G;
    G.TEMPLATE = ["TEMPLATE", 6];
    G.TEMPLATE.toString = v;
    G.TEMPLATE.__enum__ = G;
    G.TEXT = ["TEXT", 7];
    G.TEXT.toString = v;
    G.TEXT.__enum__ = G;
    var ic = function() {
        this.intervalHandle = null;
        this.isTouchScreen = !1;
        this.frameRate = null;
        ea.call(this);
        var a = this.component.style,
            b = window;
        a.position = "absolute";
        a.overflow = "hidden";
        a.left = a.top = "0";
        a.width = a.height = "100%";
        this.mousePos = new ba;
        b.addEventListener("click", F(this, this.onMouse));
        b.addEventListener("mousedown", F(this, this.onMouse));
        b.addEventListener("mouseup", F(this, this.onMouse));
        b.addEventListener("mousemove", F(this, this.onMouse));
        b.addEventListener("mousewheel", F(this, this.onWheel));
        b.addEventListener("DOMMouseScroll", F(this, this.onWheel));
        b.addEventListener("touchmove", this.getOnTouch(0));
        b.addEventListener("touchstart",
            this.getOnTouch(1));
        b.addEventListener("touchend", this.getOnTouch(2));
        b.addEventListener("touchcancel", this.getOnTouch(2));
        this.mouseMtxDepth = [];
        this.mouseMtxStack = [];
        this.mouseMtxCache = [];
        this.mouseTriggered = [];
        this.mouseUntrigger = [];
        for (a = -1; 3 > ++a;) this.mouseTriggered[a] = !1, this.mouseUntrigger[a] = this.getMouseUntrigger(a)
    };
    h["openfl.display.Stage"] = ic;
    ic.__name__ = !0;
    ic.__super__ = ea;
    ic.prototype = w(ea.prototype, {
        _broadcastMouseEvent: function(a) {
            var b = this.mouseOver,
                c;
            a.stageX = this.mousePos.x;
            a.stageY =
                this.mousePos.y;
            this.broadcastMouse(this.mouseMtxDepth, a, this.mouseMtxStack, this.mouseMtxCache);
            this.mouseOver = c = a.relatedObject;
            b != c && (null != b && b.dispatchEvent(this._alterMouseEvent(a, "mouseOut")), null != c && c.dispatchEvent(this._alterMouseEvent(a, "mouseOver")))
        },
        _broadcastTouchEvent: function(a, b, c) {
            a.stageX = b;
            a.stageY = c;
            this.broadcastMouse(this.mouseMtxDepth, a, this.mouseMtxStack, this.mouseMtxCache)
        },
        getMouseUntrigger: function(a) {
            var b = this;
            return function() {
                b.mouseTriggered[a] = !1
            }
        },
        _alterMouseEvent: function(a,
            b) {
            b = new Ta(b, a.bubbles, a.cancelable, a.localX, a.localY, a.relatedObject, a.ctrlKey, a.altKey, a.shiftKey, a.buttonDown, a.delta);
            b.stageX = a.stageX;
            b.stageY = a.stageY;
            return b
        },
        _translateMouseEvent: function(a, b) {
            return new Ta(b, !0, !1, null, null, null, a.ctrlKey, a.altKey, a.shiftKey)
        },
        _translateTouchEvent: function(a, b, c) {
            b = new jc(c, !0, !1, b.identifier, !1, null, null, b.radiusX, b.radiusY, b.force, null, a.ctrlKey, a.altKey, a.shiftKey);
            b.__jsEvent = a;
            return b
        },
        mouseEventPrevent: function(a, b, c) {
            var d = this.mousePos;
            d = d.x ==
                b && d.y == c;
            if (0 <= a && d && this.mouseTriggered[a]) return !0;
            d || this.mousePos.setTo(b, c);
            0 <= a && !this.mouseTriggered[a] && (this.mouseTriggered[a] = !0, window.setTimeout(this.mouseUntrigger[a], 0));
            1 == a ? this.mouseDown ? this._broadcastMouseEvent(this._alterMouseEvent(this.mouseLastEvent, "mouseUp")) : this.mouseDown = !0 : 2 == a && (this.mouseDown ? this.mouseDown = !1 : this._broadcastMouseEvent(new Ta("mouseDown")));
            return !1
        },
        getOnTouch: function(a) {
            var b = this;
            return function(c) {
                b.onTouch(c, a)
            }
        },
        onTouch: function(a, b) {
            var c = a.targetTouches,
                d = c.length,
                e = a.changedTouches,
                f = e.length;
            c = 0 < d ? c[0] : 0 < f ? e[0] : null;
            a.preventDefault();
            this.isTouchScreen = !0;
            null != c && (0 == b || 1 == b && d == f || 2 == b && 0 == d && 0 < f) && !this.mouseEventPrevent(b, c.pageX, c.pageY) && (this.mouseLastEvent = new Ta(1 == b ? "mouseDown" : 2 == b ? "mouseUp" : "mouseMove"), this.mouseLastEvent.__jsEvent = a, this._broadcastMouseEvent(this.mouseLastEvent), 2 == b && (d = new Ta("mouseClick"), d.__jsEvent = a, this._broadcastMouseEvent(d)));
            if (0 < f) {
                switch (b) {
                    case 1:
                        d = "touchBegin";
                        break;
                    case 2:
                        d = "touchEnd";
                        break;
                    default:
                        d =
                            "touchMove"
                }
                for (b = -1; ++b < f;) c = e[b], this._broadcastTouchEvent(this._translateTouchEvent(a, c, d), c.pageX, c.pageY)
            }
        },
        onWheel: function(a) {
            var b = this._translateMouseEvent(a, "mouseWheel"),
                c = a.wheelDelta;
            c = null != c ? 40 < Math.abs(c) ? Math.round(c / 40) : 0 > c ? -1 : 0 < c ? 1 : 0 : -a.detail;
            b.delta = c;
            this.mousePos.setTo(a.pageX, a.pageY);
            this._broadcastMouseEvent(b)
        },
        onMouse: function(a) {
            var b = null,
                c = -1;
            if ("mousemove" == a.type) b = "mouseMove", c = 0;
            else {
                var d = a.button;
                switch (a.type) {
                    case "click":
                        0 == d ? b = "mouseClick" : 1 == d ? b = "rightClick" :
                            2 == d && (b = "middleClick");
                        break;
                    case "mousedown":
                        0 == d ? b = "mouseDown" : 1 == d ? b = "middleMouseDown" : 2 == d && (b = "rightMouseDown");
                        c = 1;
                        break;
                    case "mouseup":
                        0 == d ? b = "mouseUp" : 1 == d ? b = "middleMouseUp" : 2 == d && (b = "rightMouseUp");
                        c = 2;
                        break;
                    default:
                        return
                }
            }
            this.mouseEventPrevent(c, a.pageX, a.pageY) || (this.mouseLastEvent = new Ta(b, null, null, null, null, null, a.ctrlKey, a.altKey, a.shiftKey), this.mouseLastEvent.__jsEvent = a, this._broadcastMouseEvent(this.mouseLastEvent))
        },
        hitTestLocal: function(a, b, c, d) {
            return !d || this.visible
        },
        addEventListener: function(a,
            b, c, d, e) {
            null == e && (e = !1);
            null == d && (d = 0);
            null == c && (c = !1);
            var f = this.component;
            this.component = window;
            ea.prototype.addEventListener.call(this, a, b, c, d, e);
            this.component = f
        },
        removeEventListener: function(a, b, c) {
            null == c && (c = !1);
            var d = this.component;
            this.component = window;
            ea.prototype.removeEventListener.call(this, a, b, c);
            this.component = d
        },
        set_focus: function(a) {
            null != a ? a.giveFocus() : this.component.blur();
            return a
        },
        get_stageWidth: function() {
            return window.innerWidth
        },
        get_stageHeight: function() {
            return window.innerHeight
        },
        get_stage: function() {
            return this
        },
        set_frameRate: function(a) {
            this.frameRate != a && (null != this.intervalHandle && (0 >= this.frameRate ? window._cancelAnimationFrame(this.intervalHandle) : window.clearInterval(this.intervalHandle)), 0 >= (this.frameRate = a) ? this.intervalHandle = window._requestAnimationFrame(F(this, this.onTimer)) : this.intervalHandle = window.setInterval(F(this, this.onTimer), B["int"](Math.max(0, 1E3 / a))));
            return a
        },
        onTimer: function() {
            n.getTimer();
            for (var a = -1; ++a < n.schLength;) n.schList[a](), n.schList[a] =
                null;
            n.schLength = 0;
            this.broadcastEvent(new Z("enterFrame"));
            0 >= this.frameRate && (this.intervalHandle = window._requestAnimationFrame(F(this, this.onTimer)))
        },
        __class__: ic
    });
    var oc = function(a) {
        if (null == a) throw new z("Cannot create Transform with no DisplayObject.");
        this._displayObject = a;
        this._matrix = new ia;
        this._fullMatrix = new ia;
        this.set_colorTransform(new jb)
    };
    h["openfl.geom.Transform"] = oc;
    oc.__name__ = !0;
    oc.prototype = {
        set_colorTransform: function(a) {
            return this.colorTransform = a
        },
        get_matrix: function() {
            return this._matrix.clone()
        },
        __class__: oc
    };
    var ia = function(a, b, c, d, e, f) {
        this.a = null == a ? 1 : a;
        this.b = null == b ? 0 : b;
        this.c = null == c ? 0 : c;
        this.d = null == d ? 1 : d;
        this.tx = null == e ? 0 : e;
        this.ty = null == f ? 0 : f
    };
    h["openfl.geom.Matrix"] = ia;
    ia.__name__ = !0;
    ia.create = function() {
        var a = ia.pool;
        return 0 < a.length ? a.pop() : new ia
    };
    ia.prototype = {
        clone: function() {
            return new ia(this.a, this.b, this.c, this.d, this.tx, this.ty)
        },
        identity: function() {
            this.a = this.d = 1;
            this.b = this.c = this.tx = this.ty = 0
        },
        isIdentity: function() {
            return 1 == this.a && 1 == this.d && 0 == this.tx && 0 == this.ty &&
                0 == this.b && 0 == this.c
        },
        copy: function(a) {
            this.a = a.a;
            this.b = a.b;
            this.c = a.c;
            this.d = a.d;
            this.tx = a.tx;
            this.ty = a.ty
        },
        invert: function() {
            var a = this.a * this.d - this.b * this.c;
            if (0 == a) this.a = this.b = this.c = this.d = 0, this.tx = -this.tx, this.ty = -this.ty;
            else {
                a = 1 / a;
                var b = this.d * a;
                this.d = this.a * a;
                this.a = b;
                this.b *= -a;
                this.c *= -a;
                b = -this.a * this.tx - this.c * this.ty;
                this.ty = -this.b * this.tx - this.d * this.ty;
                this.tx = b
            }
        },
        translate: function(a, b) {
            this.tx += a;
            this.ty += b
        },
        rotate: function(a) {
            var b = Math.cos(a);
            a = Math.sin(a);
            var c = this.a *
                b - this.b * a;
            this.b = this.a * a + this.b * b;
            this.a = c;
            c = this.c * b - this.d * a;
            this.d = this.c * a + this.d * b;
            this.c = c;
            c = this.tx * b - this.ty * a;
            this.ty = this.tx * a + this.ty * b;
            this.tx = c
        },
        scale: function(a, b) {
            this.a *= a;
            this.b *= b;
            this.c *= a;
            this.d *= b;
            this.tx *= a;
            this.ty *= b
        },
        concat: function(a) {
            var b = this.a * a.a + this.b * a.c;
            this.b = this.a * a.b + this.b * a.d;
            this.a = b;
            b = this.c * a.a + this.d * a.c;
            this.d = this.c * a.b + this.d * a.d;
            this.c = b;
            b = this.tx * a.a + this.ty * a.c + a.tx;
            this.ty = this.tx * a.b + this.ty * a.d + a.ty;
            this.tx = b
        },
        __class__: ia
    };
    var jb = function(a,
        b, c, d, e, f, g, m) {
        this.redMultiplier = null != a ? a : 1;
        this.greenMultiplier = null != b ? b : 1;
        this.blueMultiplier = null != c ? c : 1;
        this.alphaMultiplier = null != d ? d : 1;
        this.redOffset = null != e ? e : 0;
        this.greenOffset = null != f ? f : 0;
        this.blueOffset = null != g ? g : 0;
        this.alphaOffset = null != m ? m : 0
    };
    h["openfl.geom.ColorTransform"] = jb;
    jb.__name__ = !0;
    jb.prototype = {
        isColorSetter: function() {
            return 0 == this.redMultiplier && 0 == this.greenMultiplier && 0 == this.blueMultiplier && (0 == this.alphaMultiplier || 0 == this.alphaOffset)
        },
        isAlphaMultiplier: function() {
            return 1 ==
                this.redMultiplier && 1 == this.greenMultiplier && 1 == this.blueMultiplier && 0 == this.redOffset && 0 == this.greenOffset && 0 == this.blueOffset && 0 == this.alphaOffset
        },
        __class__: jb
    };
    var n = function() {};
    h["openfl.Lib"] = n;
    n.__name__ = !0;
    n.__init = function() {
        n.schList = [];
        n.schLength = 0;
        var a = window,
            b = "equestAnimationFrame";
        n.getTimer();
        a._requestAnimationFrame = a["r" + b] || a["webkitR" + b] || a["mozR" + b] || a["oR" + b] || a["msR" + b] || function(b) {
            return a.setTimeout(b, B["int"](700 / n.get_stage().frameRate))
        };
        b = "ancelAnimationFrame";
        a._cancelAnimationFrame =
            a["c" + b] || a["webkitC" + b] || a["mozC" + b] || a["oC" + b] || a["msC" + b] || function(b) {
                a.clearTimeout(b)
            }
    };
    n.getTimer = function() {
        return B["int"](new Date - n.qTimeStamp)
    };
    n.jsNode = function(a) {
        var b = document.createElement(a),
            c = b.style;
        c.position = "absolute";
        switch (a) {
            case "canvas":
                c.setProperty("-webkit-touch-callout", "none", null);
                n.setCSSProperties(c, "user-select", "none", 47);
                break;
            case "input":
            case "textarea":
                c.outline = "none"
        }
        return b
    };
    n.jsHelper = function() {
        if (null == n.qHelper) {
            var a = n.jsNode("div");
            n.get_stage().component.appendChild(a);
            a.style.visibility = "hidden";
            a.appendChild(n.qHelper = n.jsNode("div"))
        }
        return n.qHelper
    };
    n.get_current = function() {
        null == n.qCurrent && n.get_stage().addChild(n.qCurrent = new kc);
        return n.qCurrent
    };
    n.get_stage = function() {
        null == n.qStage && document.body.appendChild((n.qStage = new ic).component);
        return n.qStage
    };
    n.schedule = function(a) {
        n.schList[n.schLength++] = a
    };
    n.rgba = function(a) {
        return "rgba(" + (a >> 16 & 255) + "," + (a >> 8 & 255) + "," + (a & 255) + "," + ((a >> 24 & 255) / 255).toFixed(4) + ")"
    };
    n.rgbf = function(a, b) {
        return "rgba(" + (a >>
            16 & 255) + "," + (a >> 8 & 255) + "," + (a & 255) + "," + b.toFixed(4) + ")"
    };
    n.setCSSProperties = function(a, b, c, d) {
        d || (d = 31);
        d & 1 && a.setProperty(b, c, null);
        d & 2 && a.setProperty("-webkit-" + b, c, null);
        d & 4 && a.setProperty("-moz-" + b, c, null);
        d & 8 && a.setProperty("-ms-" + b, c, null);
        d & 16 && a.setProperty("-o-" + b, c, null);
        d & 32 && a.setProperty("-khtml-" + b, c, null)
    };
    var Fc = function() {};
    h["openfl.bitfive.NodeTools"] = Fc;
    Fc.__name__ = !0;
    Fc.createCanvasElement = function() {
        var a = window.document.createElement("canvas");
        var b = a.style;
        b.position = "absolute";
        b.setProperty("-webkit-touch-callout", "none", null);
        Kc.setProperties(b, "user-select", "none", 63);
        return a
    };
    var Kc = function() {};
    h["openfl.bitfive.StyleTools"] = Kc;
    Kc.__name__ = !0;
    Kc.setProperties = function(a, b, c, d) {
        null == d && (d = 31);
        d & 1 && a.setProperty("" + b, c, null);
        d & 2 && a.setProperty("-webkit-" + b, c, null);
        d & 4 && a.setProperty("-moz-" + b, c, null);
        d & 8 && a.setProperty("-ms-" + b, c, null);
        d & 16 && a.setProperty("-o-" + b, c, null);
        d & 32 && a.setProperty("-khtml-" + b, c, null)
    };
    var xb = function(a, b) {
        this.component = n.jsNode("a");
        ea.call(this);
        this.set_href(a);
        this.set_target(b)
    };
    h["openfl.display.Anchor"] = xb;
    xb.__name__ = !0;
    xb.__super__ = fa;
    xb.prototype = w(fa.prototype, {
        set_href: function(a) {
            this.href != a && (this.component.href = this.href = a);
            return a
        },
        set_target: function(a) {
            this.target != a && (this.component.target = this.target = a);
            return a
        },
        __class__: xb
    });
    var kb = function(a, b, c) {
        aa.call(this);
        this.set_bitmapData(a)
    };
    h["openfl.display.Bitmap"] = kb;
    kb.__name__ = !0;
    kb.__interfaces__ = [Qa];
    kb.__super__ = aa;
    kb.prototype = w(aa.prototype, {
        set_bitmapData: function(a) {
            null !=
                this.bitmapData && this.component.removeChild(this.bitmapData.component);
            null != a && this.component.appendChild(a.handle());
            return this.bitmapData = a
        },
        get_width: function() {
            return null != this.__width ? this.__width : null != this.bitmapData ? this.bitmapData.component.width : 0
        },
        get_height: function() {
            return null != this.__height ? this.__height : null != this.bitmapData ? this.bitmapData.component.height : 0
        },
        drawToSurface: function(a, b, c, d, e, f, g) {
            this.bitmapData.drawToSurface(a, b, c, d, e, f, g)
        },
        hitTestLocal: function(a, b, c, d) {
            return (!d ||
                this.visible) && null != this.bitmapData && 0 <= a && 0 <= b && a < this.bitmapData.component.width && b < this.bitmapData.component.height
        },
        __class__: kb
    });
    var Lc = function() {};
    h["openfl.display.IGraphics"] = Lc;
    Lc.__name__ = !0;
    Lc.__interfaces__ = [Qa];
    var tb = function() {
        this.rgPending = !1;
        this.synced = !0;
        this.component = n.jsNode("canvas");
        this.context = this.component.getContext("2d", null);
        this.context.save();
        this.bounds = new Ia;
        this.resetBounds();
        this.irec = [];
        this.frec = [];
        this.arec = [];
        this.lineWidth = this.ilen = this.flen = this.alen =
            0
    };
    h["openfl.display.Graphics"] = tb;
    tb.__name__ = !0;
    tb.__interfaces__ = [Lc, Qa];
    tb.prototype = {
        regenerate: function() {
            var a = this.component,
                b = this.component.style,
                c = this.context,
                d = this.bounds,
                e = ~~(d.x - 2),
                f = ~~(d.y - 2),
                g = Math.ceil(d.width + 4),
                m = Math.ceil(d.height + 4);
            this.synced = !0;
            this.rgPending = !1;
            if (0 >= d.width || 0 >= d.height) a.width = a.height = 1, b.top = b.left = "0";
            else {
                if (this.offsetX != e || this.offsetY != f) b.left = (this.offsetX = e) + "px", b.top = (this.offsetY = f) + "px";
                g != a.width || m != a.height ? (a.width = g, a.height = m) : c.clearRect(0,
                    0, g, m);
                c.save();
                c.translate(-e, -f);
                this.render(a, c);
                c.restore()
            }
        },
        regenerateTask: function() {
            this.rgPending && this.regenerate()
        },
        requestRegeneration: function() {
            n.schedule(F(this, this.regenerateTask));
            this.rgPending = !0
        },
        set_displayObject: function(a) {
            this.displayObject != a && (this.displayObject = a, this.synced || this.requestRegeneration());
            return a
        },
        resetBounds: function() {
            this.bounds.setVoid();
            this.invalidate()
        },
        grab: function(a, b, c, d) {
            var e;
            a < (e = this.bounds.x) && (e -= a, this.bounds.x -= e, this.bounds.width += e);
            b < (e = this.bounds.y) && (e -= b, this.bounds.y -= e, this.bounds.height += e);
            c > (e = this.bounds.get_right()) && (this.bounds.width += c - e);
            d > (e = this.bounds.get_bottom()) && (this.bounds.height += d - e);
            this.invalidate()
        },
        invalidate: function() {
            this.synced && (this.synced = !1, this.rgPending || null == this.displayObject || null == this.displayObject.get_stage() || this.requestRegeneration())
        },
        clear: function() {
            for (var a = 0; a < this.alen;) this.arec[a++] = null;
            this.lineWidth = this.ilen = this.flen = this.alen = 0;
            this.resetBounds();
            this.invalidate()
        },
        beginFill: function(a, b) {
            this.irec[this.ilen++] = 2;
            a = n.rgbf(null != a ? a : 0, null != b ? b : 1);
            this.arec[this.alen++] = a
        },
        endFill: function() {
            this.irec[this.ilen++] = 9;
            this.invalidate()
        },
        drawRect: function(a, b, c, d) {
            this.irec[this.ilen++] = 13;
            var e = this.frec,
                f = this.flen;
            e[f++] = a;
            e[f++] = b;
            e[f++] = c;
            e[f++] = d;
            this.flen = f;
            e = this.lineWidth / 2;
            this.grab(a - e, b - e, a + c + e, b + d + e)
        },
        drawCircle: function(a, b, c) {
            this.irec[this.ilen++] = 14;
            var d = this.frec,
                e = this.flen;
            d[e++] = a;
            d[e++] = b;
            d[e++] = c;
            this.flen = e;
            c += this.lineWidth / 2;
            this.grab(a -
                c, b - c, a + c, b + c)
        },
        drawToSurface: function(a, b, c, d, e, f, g) {
            b.save();
            null != c && b.transform(c.a, c.b, c.c, c.d, c.tx, c.ty);
            this.render(a, b);
            b.restore()
        },
        hitTestLocal: function(a, b, c) {
            if (this.bounds.contains(a, b)) {
                if (c) {
                    this.synced || this.regenerate();
                    try {
                        return 0 != this.context.getImageData(a - this.offsetX, b - this.offsetY, 1, 1).data[3]
                    } catch (d) {
                        d instanceof z && (d = d.val)
                    }
                }
                return !0
            }
            return !1
        },
        _closePath: function(a, b, c, d, e) {
            c & 1 && (b.closePath(), c & 4 ? (b.save(), b.transform(d.a, d.b, d.c, d.d, d.tx, d.ty), b.fillStyle = b.createPattern(e,
                c & 8 ? "repeat" : "no-repeat"), b.fill(), b.restore()) : b.fill());
            c & 2 && b.stroke();
            b.beginPath();
            return c
        },
        render: function(a, b) {
            var c = 0,
                d = this._drawMatrix,
                e, f, g, m = 0,
                h = null,
                k = this.irec,
                l = -1,
                q = k.length - 1,
                t = this.arec,
                w = -1,
                n = this.frec,
                p = -1;
            null == d && (this._drawMatrix = d = new ia);
            for (b.save(); l < q;) switch (f = k[++l]) {
                case 1:
                    0 < m && (c = this._closePath(a, b, c, d, h));
                    b.lineWidth = g = n[++p];
                    0 < g ? (c |= 2, b.strokeStyle = t[++w], 2 == (f = k[++l]) ? b.lineCap = "butt" : b.lineCap = 1 == f ? "square" : "round", 2 == (f = k[++l]) ? b.lineJoin = "bevel" : b.lineJoin =
                        1 == f ? "miter" : "round") : (c &= -3, b.strokeStyle = null);
                    break;
                case 2:
                case 3:
                case 4:
                    0 < m && (c = this._closePath(a, b, c, d, h));
                    c |= 1;
                    3 == f ? (h = t[++w].handle(), f = k[++l], 0 != k[++l] ? (c = 0 != f ? c | 8 : c & -9, d.a = n[++p], d.b = n[++p], d.c = n[++p], d.d = n[++p], d.tx = n[++p], d.ty = n[++p], c |= 4) : (b.fillStyle = b.createPattern(h, 0 != f ? "repeat" : "no-repeat"), c &= -5)) : (b.fillStyle = t[++w], c &= -5);
                    m = 0;
                    break;
                case 9:
                    0 < m && (c = this._closePath(a, b, c, d, h), m = 0);
                    c &= -2;
                    break;
                case 10:
                    b.moveTo(n[++p], n[++p]);
                    m++;
                    break;
                case 11:
                    b.lineTo(n[++p], n[++p]);
                    m++;
                    break;
                case 12:
                    b.quadraticCurveTo(n[++p],
                        n[++p], n[++p], n[++p]);
                    m++;
                    break;
                case 13:
                    b.rect(n[++p], n[++p], n[++p], n[++p]);
                    m++;
                    break;
                case 14:
                    f = n[++p];
                    g = n[++p];
                    var r = n[++p];
                    0 > r && (r = -r);
                    b.moveTo(f + r, g);
                    0 != r && b.arc(f, g, r, 0, 2 * Math.PI, !0);
                    m++;
                    break;
                case 17:
                    f = n[++p];
                    g = n[++p];
                    var A = n[++p],
                        y = n[++p];
                    r = f + A / 2;
                    var u = g + y / 2,
                        v = f + A,
                        B = g + y;
                    A *= .275892;
                    y *= .275892;
                    b.moveTo(r, g);
                    b.bezierCurveTo(r + A, g, v, u - y, v, u);
                    b.bezierCurveTo(v, u + y, r + A, B, r, B);
                    b.bezierCurveTo(r - A, B, f, u + y, f, u);
                    b.bezierCurveTo(f, u - y, r - A, g, r, g);
                    m++;
                    break;
                case 15:
                    f = n[++p];
                    g = n[++p];
                    r = n[++p];
                    u = n[++p];
                    v =
                        n[++p];
                    B = n[++p];
                    null == B ? (b.moveTo(f + v, g + u), b.arcTo(f + r - v, g + u - v, f + r, g + u - v, v), b.arcTo(f + r, g + v, f + r - v, g, v), b.arcTo(f + v, g, f, g + v, v), b.arcTo(f + v, g + u - v, f + v, g + u, v)) : (b.moveTo(f + v, g + u), b.lineTo(f + r - v, g + u), b.quadraticCurveTo(f + r, g + u, f + r, g + u - B), b.lineTo(f + r, g + B), b.quadraticCurveTo(f + r, g, f + r - v, g), b.lineTo(f + v, g), b.quadraticCurveTo(f, g, f, g + B), b.lineTo(f, g + u - B), b.quadraticCurveTo(f, g + u, f + v, g + u));
                    m++;
                    break;
                case 16:
                    f = t[++w].handle();
                    y = k[++l];
                    g = 0 != (y & 1);
                    r = 0 != (y & 2);
                    u = 0 != (y & 8);
                    v = 0 != (y & 16);
                    B = k[++l];
                    b.save();
                    for (b.globalCompositeOperation =
                        0 != (y & 65536) ? "lighter" : "source-over"; 0 <= --B;) {
                        y = n[++p];
                        A = n[++p];
                        var D = n[++p];
                        var E = n[++p];
                        var F = n[++p];
                        var H = n[++p];
                        var G = n[++p];
                        var I = n[++p];
                        b.save();
                        v ? b.transform(n[++p], n[++p], n[++p], n[++p], y, A) : (b.translate(y, A), g && b.scale(e = n[++p], e), r && b.rotate(n[++p]));
                        u && (b.globalAlpha = n[++p]);
                        b.drawImage(f, F, H, G, I, -D, -E, G, I);
                        b.restore()
                    }
                    b.restore();
                    break;
                default:
                    throw new z(4E3 + f);
            }
            0 < m && this._closePath(a, b, c, d, h);
            b.restore()
        },
        __class__: tb
    };
    var zc = function() {};
    h["openfl.display.ILoader"] = zc;
    zc.__name__ = !0;
    zc.prototype = {
        __class__: zc
    };
    var sb = function() {
        ea.call(this);
        this.contentLoaderInfo = Ua.create(this)
    };
    h["openfl.display.Loader"] = sb;
    sb.__name__ = !0;
    sb.__interfaces__ = [zc];
    sb.__super__ = fa;
    sb.prototype = w(fa.prototype, {
        load: function(a, b) {
            b = a.url.split(".");
            0 < b.length && b[b.length - 1].toLowerCase();
            b = a.url;
            var c = b.lastIndexOf(".");
            if (0 > c) throw new z('Extension must be specified, got "' + b + '"');
            var d = !0;
            c = b.substring(c + 1);
            switch (c) {
                case "swf":
                    c = "application/x-shockwave-flash";
                    break;
                case "png":
                    c = "image/png";
                    break;
                case "gif":
                    c = "image/gif";
                    break;
                case "jpg":
                case "jpeg":
                    d = !1;
                    c = "image/jpeg";
                    break;
                default:
                    throw new z('Unrecognized extension "' + c + '" in "' + b + '"');
            }
            this.contentLoaderInfo.url = b;
            this.contentLoaderInfo.contentType = c;
            this.mImage = new ca(0, 0, d);
            try {
                this.contentLoaderInfo.addEventListener("complete", F(this, this.handleLoad), !1), this.mImage.nmeLoadFromFile(a.url, this.contentLoaderInfo), this.content = new kb(this.mImage), this.contentLoaderInfo.content = this.content, this.addChild(this.content)
            } catch (e) {
                e instanceof
                z && (e = e.val);
                Rc.log("Error " + B.string(e));
                a = new Cb("ioError");
                a.set_currentTarget(this);
                this.contentLoaderInfo.dispatchEvent(a);
                return
            }
            null == this.mShape && (this.mShape = new bb, this.addChild(this.mShape))
        },
        handleLoad: function(a) {
            a.set_currentTarget(this);
            this.contentLoaderInfo.removeEventListener("complete", F(this, this.handleLoad))
        },
        __class__: sb
    });
    var Ua = function() {
        this.eventList = new Y;
        this.bytesLoaded = this.bytesTotal = 0;
        this.childAllowsParent = !0;
        this.parameters = {}
    };
    h["openfl.display.LoaderInfo"] = Ua;
    Ua.__name__ = !0;
    Ua.create = function(a) {
        var b = new Ua;
        null != a ? b.loader = a : b.url = "";
        return b
    };
    Ua.__super__ = ta;
    Ua.prototype = w(ta.prototype, {
        __class__: Ua
    });
    var kc = function() {
        ea.call(this);
        this.enabled = !0;
        this.qIndex = this.qTotal = 0;
        this.loaderInfo = Ua.create()
    };
    h["openfl.display.MovieClip"] = kc;
    kc.__name__ = !0;
    kc.__super__ = fa;
    kc.prototype = w(fa.prototype, {
        __class__: kc
    });
    var yb = function(a, b, c, d) {
        null == d && (d = 0);
        null == c && (c = 0);
        null == b && (b = 0);
        null == a && (a = 0);
        ea.call(this);
        this.set_width(a);
        this.set_height(b);
        this.set_color(c);
        this.set_radius(d)
    };
    h["openfl.display.RoundRect"] = yb;
    yb.__name__ = !0;
    yb.__super__ = fa;
    yb.prototype = w(fa.prototype, {
        get_width: function() {
            return this.__width
        },
        get_height: function() {
            return this.__height
        },
        set_width: function(a) {
            this.__width != a && (this.__width = a, this.component.style.width = a + "px");
            return a
        },
        set_height: function(a) {
            this.__height != a && (this.__height = a, this.component.style.height = a + "px");
            return a
        },
        set_color: function(a) {
            this.__color != a && (this.__color = a, this.component.style.backgroundColor = n.rgba(a));
            return a
        },
        set_radius: function(a) {
            this.__radius != a && (this.__radius = a, this.component.style.borderRadius = 0 < a ? a + "px" : "");
            return a
        },
        hitTestLocal: function(a, b, c, d) {
            return fa.prototype.hitTestLocal.call(this, a, b, c, d) ? !0 : (!d || this.visible) && 0 <= a && 0 <= b && a < this.__width && b < this.__height
        },
        __class__: yb
    });
    var bb = function() {
        (this.graphics = new tb).set_displayObject(this);
        this.component = this.graphics.component;
        aa.call(this)
    };
    h["openfl.display.Shape"] = bb;
    bb.__name__ = !0;
    bb.__interfaces__ = [Qa];
    bb.__super__ = aa;
    bb.prototype =
        w(aa.prototype, {
            drawToSurface: function(a, b, c, d, e, f, g) {
                this.graphics.drawToSurface(a, b, c, d, e, f, g)
            },
            set_stage: function(a) {
                var b = null == this.get_stage() && null != a;
                a = aa.prototype.set_stage.call(this, a);
                b && this.graphics.invalidate();
                return a
            },
            hitTestLocal: function(a, b, c, d) {
                return (!d || this.visible) && this.graphics.hitTestLocal(a, b, c)
            },
            __class__: bb
        });
    var Z = function(a, b, c) {
        null == c && (c = !1);
        null == b && (b = !1);
        this.type = a;
        this.bubbles = b;
        this.cancelable = c
    };
    h["openfl.events.Event"] = Z;
    Z.__name__ = !0;
    Z.prototype = {
        get_target: function() {
            return this._target ||
                this.target
        },
        set_target: function(a) {
            return this._target = a
        },
        set_currentTarget: function(a) {
            return this._current = a
        },
        __class__: Z
    };
    var Cb = function(a, b, c, d) {
        null == d && (d = "");
        null == c && (c = !1);
        null == b && (b = !1);
        Z.call(this, a, b, c);
        this.text = d
    };
    h["openfl.events.IOErrorEvent"] = Cb;
    Cb.__name__ = !0;
    Cb.__super__ = Z;
    Cb.prototype = w(Z.prototype, {
        __class__: Cb
    });
    var Ac = function() {};
    h["openfl.events.KeyboardEvent"] = Ac;
    Ac.__name__ = !0;
    Ac.__super__ = Z;
    Ac.prototype = w(Z.prototype, {
        __class__: Ac
    });
    var gb = function(a, b, c) {
        Z.call(this,
            a, b, c)
    };
    h["openfl.events.UIEvent"] = gb;
    gb.__name__ = !0;
    gb.__super__ = Z;
    gb.prototype = w(Z.prototype, {
        __class__: gb
    });
    var Ta = function(a, b, c, d, e, f, g, m, h, k, l) {
        Z.call(this, a, null != b ? b : !0, null != c ? c : !1);
        this.ctrlKey = null != g ? g : !1;
        this.altKey = null != m ? m : !1;
        this.shiftKey = null != h ? h : !1;
        this.relatedObject = f;
        this.buttonDown = null != k ? k : !1;
        this.delta = null != l ? l : 0
    };
    h["openfl.events.MouseEvent"] = Ta;
    Ta.__name__ = !0;
    Ta.__super__ = gb;
    Ta.prototype = w(gb.prototype, {
        __class__: Ta
    });
    var jc = function(a, b, c, d, e, f, g, m, h, k, l, n, p, q) {
        Z.call(this,
            a, b, c);
        this.altKey = p;
        this.shiftKey = q;
        this.ctrlKey = n;
        this.touchPointID = d;
        this.sizeX = m;
        this.sizeY = h;
        this.pressure = k
    };
    h["openfl.events.TouchEvent"] = jc;
    jc.__name__ = !0;
    jc.__super__ = gb;
    jc.prototype = w(gb.prototype, {
        __class__: jc
    });
    var Vc = function() {};
    h["openfl.filters.BitmapFilter"] = Vc;
    Vc.__name__ = !0;
    var Ia = function(a, b, c, d) {
        null == d && (d = 0);
        null == c && (c = 0);
        null == b && (b = 0);
        null == a && (a = 0);
        this.x = a;
        this.y = b;
        this.width = c;
        this.height = d
    };
    h["openfl.geom.Rectangle"] = Ia;
    Ia.__name__ = !0;
    Ia.prototype = {
        clone: function() {
            return new Ia(this.x,
                this.y, this.width, this.height)
        },
        equals: function(a) {
            return this.x == a.x && this.y == a.y && this.width == a.width && this.height == a.height
        },
        setVoid: function() {
            this.width -= 2147483647 - this.x;
            this.x = 2147483647;
            this.width = -2147483648 - this.x; - 2147483648;
            this.height -= 2147483647 - this.y;
            this.y = 2147483647;
            this.height = -2147483648 - this.y; - 2147483648
        },
        get_right: function() {
            return this.x + this.width
        },
        get_bottom: function() {
            return this.y + this.height
        },
        contains: function(a, b) {
            return 0 <= (a -= this.x) && 0 <= (b -= this.y) && a < this.width &&
                b < this.height
        },
        __class__: Ia
    };
    var Bc = function() {};
    h["openfl.media.Sound"] = Bc;
    Bc.__name__ = !0;
    Bc.__super__ = ta;
    Bc.prototype = w(ta.prototype, {
        __class__: Bc
    });
    var Ka = function(a, b, c) {
        this.description = a;
        this.extension = b;
        this.macType = c
    };
    h["openfl.net.FileFilter"] = Ka;
    Ka.__name__ = !0;
    Ka.prototype = {
        __class__: Ka
    };
    var Ya = function() {
        this.eventList = new Y
    };
    h["openfl.net.FileReference"] = Ya;
    Ya.__name__ = !0;
    Ya.__super__ = ta;
    Ya.prototype = w(ta.prototype, {
        browse: function(a) {
            var b = n.jsHelper(),
                c, d = this.fileForm,
                e, f, g;
            null == d ?
                (this.fileInput = c = document.createElement("input"), this.fileForm = d = document.createElement("form"), this.fileForm.appendChild(c), c.type = "file", c.onchange = F(this, this.onFileChange)) : (c = this.fileInput, d.reset());
            var m = "";
            if (null != a) {
                var h = -1;
                for (e = a.length; ++h < e;) "" != m && (m += ";"), m += a[h].extension;
                a = m.split(";");
                m = "";
                h = -1;
                for (e = a.length; ++h < e;) - 1 != (f = a[h].lastIndexOf(".")) && ".*" != (g = y.substr(a[h], f, null)) && ("" != m && (m += ","), m += g)
            }
            c.accept = m;
            b.appendChild(d);
            c.click();
            b.removeChild(d);
            return !0
        },
        save: function(a,
            b) {
            null == b && (b = "");
            var c = this.fileLink,
                d = n.jsHelper();
            null == c && (this.fileLink = c = document.createElement("a"));
            try {
                var e = new Blob([a.byteView], {
                    type: "application/octet-stream"
                });
                var f = window.navigator;
                if (null != f.msSaveBlob) {
                    f.msSaveBlob(e, b);
                    return
                }
                if (null != window.saveAs) try {
                    window.saveAs(e, b);
                    return
                } catch (g) {
                    g instanceof z && (g = g.val)
                }
                c.href = URL.createObjectURL(e)
            } catch (g) {
                g instanceof z && (g = g.val), c.href = "data:application/octet-stream;base64," + a.toBase64()
            }
            c.target = "_blank";
            c.download = b;
            c.setAttribute("download",
                b);
            d.appendChild(c);
            c.click();
            d.removeChild(c)
        },
        load: function() {
            var a = this;
            if (null != this.file) try {
                var b = new FileReader;
                b.readAsArrayBuffer(this.file);
                this.data = null;
                this.dispatchEvent(new Z("open"));
                b.onload = function(c) {
                    a.data = Aa.nmeOfBuffer(b.result);
                    a.dispatchEvent(new Z("complete"))
                };
                b.onerror = function(b) {
                    a.dispatchEvent(new Cb("ioError", !1, !1, "Failed to load the file."))
                }
            } catch (c) {
                throw c instanceof z && (c = c.val), new z("Failed to dispatch FileReader.");
            }
        },
        onFileChange: function(a) {
            this.file = this.fileInput.files[0];
            null != this.file && this.dispatchEvent(new Z("select"))
        },
        get_name: function() {
            return this.file.name
        },
        __class__: Ya
    });
    var Cc = function() {};
    h["openfl.net.IURLLoader"] = Cc;
    Cc.__name__ = !0;
    Cc.__interfaces__ = [Hb];
    Cc.prototype = {
        __class__: Cc
    };
    var Fb = function(a) {
        null != a && (this.url = a);
        this.requestHeaders = [];
        this.method = "GET";
        this.contentType = null
    };
    h["openfl.net.URLRequest"] = Fb;
    Fb.__name__ = !0;
    Fb.prototype = {
        __class__: Fb
    };
    var Wc = function() {};
    h["openfl.net.URLRequestHeader"] = Wc;
    Wc.__name__ = !0;
    var Xc = function() {};
    h["openfl.text.Font"] =
        Xc;
    Xc.__name__ = !0;
    var Ab = function() {
        this.__editable = !1;
        this.__text = "";
        this.__autoSize = -1;
        this.multiline = this.wordWrap = !1;
        this.maxChars = 0;
        this.border = !1;
        ua.call(this);
        var a = this.component.style;
        a.whiteSpace = "nowrap";
        a.overflow = "hidden";
        a.padding = "1.5px";
        this.__textFormat = new qb("Times New Roman", 12, 0, !1, !1, !1, "", "", "LEFT", 0, 0, 0, 0);
        this.__height = this.__width = 100;
        this.__applySize(3);
        this.__applyTextFormat()
    };
    h["openfl.text.TextField"] = Ab;
    Ab.__name__ = !0;
    Ab.__interfaces__ = [Qa];
    Ab.__super__ = ua;
    Ab.prototype =
        w(ua.prototype, {
            get_defaultTextFormat: function() {
                return this.__textFormat.clone()
            },
            __applyType: function(a) {
                var b = this.component,
                    c = this.get_text();
                (this.__editable = a) ? (a = n.jsNode(this.multiline ? "textarea" : "input"), a.value = c, a.maxLength = 0 < this.maxChars ? this.maxChars : 2147483647, c = a.style, c.border = "0", c.padding = "0", c.background = "transparent", b.appendChild(this.__field = a)) : (b.removeChild(this.__field), this.__field = null)
            },
            __applyTextFormat: function() {
                this.__textFormatSync = !0;
                var a = this.__textFormat;
                var b =
                    (this.__editable ? this.__field : this.component).style;
                this.__fontStyle = b.font = a.get_fontStyle();
                b.lineHeight = "1.25";
                b.textAlign = a.align;
                b.fontWeight = a.bold ? "bold" : "";
                b.fontStyle = a.italic ? "italic" : "";
                b.textDecoration = a.underline ? "underline" : "";
                b.color = n.rgbf(a.color, 1)
            },
            __applyText: function(a) {
                this.__text = a;
                this.__editable ? this.__field.value = a : null == this.component.innerText ? this.component.innerHTML = D.replace(D.htmlEscape(a), "\n", "<br>") : this.component.innerText = a;
                this.__applyAutoSize()
            },
            __applySize: function(a) {
                var b =
                    this.component.style,
                    c = this.__editable;
                var d = c ? this.__field.style : null;
                for (var e = 1; 4 > e;) {
                    if (0 != (a & e)) {
                        var f = 1 == e ? this.__width : this.__height;
                        1 == e && 0 <= this.__autoSize && !this.wordWrap && (f = null);
                        null != f ? (this.border && --f, f -= 3, f += "px") : f = "";
                        1 == e ? (b.width = f, c && (d.width = f)) : (b.height = f, c && (d.height = f))
                    }
                    e <<= 1
                }
            },
            get_text: function() {
                return this.__editable ? this.__field.value : this.__text
            },
            set_text: function(a) {
                this.get_text() != a && this.__applyText(a);
                this.__textFormatSync || this.__applyTextFormat();
                return a
            },
            setSelection: function(a,
                b) {
                this.__editable && this.__field.setSelectionRange(a, b)
            },
            drawToSurface: function(a, b, c, d, e, f, g) {
                b.save();
                b.fillStyle = this.component.style.color;
                b.font = this.__fontStyle;
                b.textBaseline = "top";
                b.textAlign = this.__textFormat.align;
                b.fillText(this.get_text(), 0, 0);
                b.restore()
            },
            get_width: function() {
                return 0 > this.__autoSize ? this.__width : this.get_textWidth()
            },
            get_height: function() {
                return 0 > this.__autoSize ? this.__height : this.get_textHeight()
            },
            set_height: function(a) {
                this.__height != a && (this.__height = a, this.__applySize(2));
                return a
            },
            __measurePre: function() {
                var a = n.jsHelper();
                a.setAttribute("style", this.component.getAttribute("style"));
                var b = a.style;
                this.wordWrap || (b.width = "");
                b.height = "";
                b.paddingTop = "";
                b.paddingBottom = "";
                b.borderTop = "";
                b.borderBottom = "";
                a.innerHTML = this.component.innerHTML;
                return a
            },
            __measurePost: function(a) {
                a.setAttribute("style", "");
                a.innerHTML = ""
            },
            get_textWidth: function() {
                if (null == this.get_stage()) {
                    var a = this.__measurePre(),
                        b = a.clientWidth;
                    this.__measurePost(a);
                    return b
                }
                return this.component.clientWidth
            },
            get_textHeight: function() {
                if (null == this.get_stage()) {
                    var a = this.__measurePre(),
                        b = a.clientHeight;
                    this.__measurePost(a);
                    return b
                }
                return this.component.clientHeight
            },
            __applyAutoSize: function() {
                var a = this.__autoSize,
                    b = this.component.style;
                0 <= a && !this.__editable ? (b.left = 0 < a ? (this.__width - this.get_textWidth()) * a / 2 + "px" : "", this.wordWrap || (b.width = ""), b.height = "") : (b.left = "", this.__applySize(3))
            },
            set_type: function(a) {
                var b = "INPUT" == a;
                b != this.__editable && this.__applyType(b);
                return a
            },
            set_multiline: function(a) {
                this.multiline !=
                    a && (this.multiline = a, this.__editable && this.__applyType(!0));
                return a
            },
            giveFocus: function() {
                (this.__editable ? this.__field : this.component).focus()
            },
            get_selectionBeginIndex: function() {
                return this.__editable ? this.__field.selectionStart : 0
            },
            get_selectionEndIndex: function() {
                return this.__editable ? this.__field.selectionEnd : 0
            },
            hitTestLocal: function(a, b, c, d) {
                return (!d || this.visible) && 0 <= a && 0 <= b && a < this.get_width() && b < this.get_height()
            },
            addEventListener: function(a, b, c, d, e) {
                null == e && (e = !1);
                null == d && (d = 0);
                null ==
                    c && (c = !1);
                var f = this.component;
                this.__editable && (this.component = this.__field);
                ua.prototype.addEventListener.call(this, a, b, c, d, e);
                this.__editable && (this.component = f)
            },
            removeEventListener: function(a, b, c) {
                null == c && (c = !1);
                var d = this.component;
                this.__editable && (this.component = this.__field);
                ua.prototype.removeEventListener.call(this, a, b, c);
                this.__editable && (this.component = d)
            },
            __class__: Ab
        });
    var qb = function(a, b, c, d, e, f, g, m, h, k, l, n, p) {
        this.font = a;
        this.size = b;
        this.color = c;
        this.bold = d;
        this.italic = e;
        this.underline =
            f;
        this.url = g;
        this.target = m;
        this.align = h;
        this.leftMargin = k;
        this.rightMargin = l;
        this.indent = n;
        this.leading = p;
        this.tabStops = []
    };
    h["openfl.text.TextFormat"] = qb;
    qb.__name__ = !0;
    qb.translateFont = function(a) {
        switch (a) {
            case "_sans":
                return "sans-serif";
            case "_serif":
                return "serif";
            case "_typewriter":
                return "monospace";
            default:
                return null == a ? "sans-serif" : a
        }
    };
    qb.prototype = {
        clone: function() {
            var a = new qb(this.font, this.size, this.color, this.bold, this.italic, this.underline, this.url, this.target, this.align, this.leftMargin,
                this.rightMargin, this.indent, this.leading);
            a.blockIndent = this.blockIndent;
            a.bullet = this.bullet;
            a.indent = this.indent;
            a.kerning = this.kerning;
            a.letterSpacing = this.letterSpacing;
            a.tabStops = this.tabStops.slice(0);
            return a
        },
        get_fontStyle: function() {
            return (this.bold ? "bold " : "") + (this.italic ? "italic " : "") + this.size + "px " + qb.translateFont(this.font)
        },
        __class__: qb
    };
    var Aa = function() {
        this.littleEndian = !1;
        this.length = 0;
        this._nmeResizeBuffer(this.allocated = this.position = 0)
    };
    h["openfl.utils.ByteArray"] = Aa;
    Aa.__name__ = !0;
    Aa.nmeOfBuffer = function(a) {
        var b = new Aa;
        b.set_length(b.allocated = a.byteLength);
        b.data = new Yc(a);
        b.byteView = new dc(a);
        return b
    };
    Aa.prototype = {
        _nmeResizeBuffer: function(a) {
            var b = this.byteView,
                c = new dc(a);
            null != b && (b.length <= a ? c.set(b) : c.set(b.subarray(0, a)));
            this.byteView = c;
            this.data = new Yc(c.buffer)
        },
        clear: function() {
            this.set_length(this.position = 0)
        },
        readByte: function() {
            return this.data.getUint8(this.position++)
        },
        readFloat: function() {
            var a = this.data.getFloat32(this.position, this.littleEndian);
            this.position +=
                4;
            return a
        },
        readInt: function() {
            var a = this.data.getInt32(this.position, this.littleEndian);
            this.position += 4;
            return a
        },
        readShort: function() {
            var a = this.data.getInt16(this.position, this.littleEndian);
            this.position += 2;
            return a
        },
        readUnsignedInt: function() {
            var a = this.data.getUint32(this.position, this.littleEndian);
            this.position += 4;
            return a
        },
        readUnsignedShort: function() {
            var a = this.data.getUint16(this.position, this.littleEndian);
            this.position += 2;
            return a
        },
        readUTF: function() {
            return this.readUTFBytes(this.readUnsignedShort())
        },
        readUTFBytes: function(a) {
            var b = "";
            for (a = this.position + a; this.position < a;) {
                var c = this.data.getUint8(this.position++);
                if (128 > c) {
                    if (0 == c) break;
                    b += String.fromCharCode(c)
                } else if (224 > c) b += String.fromCharCode((c & 63) << 6 | this.data.getUint8(this.position++) & 127);
                else if (240 > c) {
                    var d = this.data.getUint8(this.position++);
                    b += String.fromCharCode((c & 31) << 12 | (d & 127) << 6 | this.data.getUint8(this.position++) & 127)
                } else {
                    d = this.data.getUint8(this.position++);
                    var e = this.data.getUint8(this.position++);
                    b += String.fromCharCode((c &
                        15) << 18 | (d & 127) << 12 | e << 6 & 127 | this.data.getUint8(this.position++) & 127)
                }
            }
            return b
        },
        toBase64: function() {
            for (var a = this.length, b = -1, c = this.data, d = "", e, f, g; ++b < a;) e = c.getUint8(b), f = ++b < a ? c.getUint8(b) : 0, g = ++b < a ? c.getUint8(b) : 0, d += "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".charAt(e >> 2) + "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".charAt((e & 3) << 4 | f >> 4) + (b - 1 < a ? "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".charAt((f & 15) << 2 | g >> 6) : "=") + (b <
                a ? "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".charAt(g & 63) : "=");
            return d
        },
        writeByte: function(a) {
            var b = this.position + 1;
            this.length < b && this.set_length(b);
            this.data.setInt8(this.position, a);
            this.position += 1
        },
        writeFloat: function(a) {
            var b = this.position + 4;
            this.length < b && this.set_length(b);
            this.data.setFloat32(this.position, a, this.littleEndian);
            this.position += 4
        },
        writeInt: function(a) {
            var b = this.position + 4;
            this.length < b && this.set_length(b);
            this.data.setInt32(this.position, a, this.littleEndian);
            this.position += 4
        },
        writeShort: function(a) {
            var b = this.position + 2;
            this.length < b && this.set_length(b);
            this.data.setInt16(this.position, a, this.littleEndian);
            this.position += 2
        },
        writeUnsignedInt: function(a) {
            var b = this.position + 4;
            this.length < b && this.set_length(b);
            this.data.setUint32(this.position, a, this.littleEndian);
            this.position += 4
        },
        writeUTFBytes: function(a) {
            for (var b = -1, c = a.length, d; ++b < c;) d = a.charCodeAt(b), 127 >= d ? this.writeByte(d) : (2047 >= d ? this.writeByte(192 | d >> 6) : (65535 >= d ? this.writeByte(224 | d >> 12) : (this.writeByte(240 |
                d >> 18), this.writeByte(128 | d >> 12 & 63)), this.writeByte(128 | d >> 6 & 63)), this.writeByte(128 | d & 63))
        },
        set_length: function(a) {
            this.allocated < a ? this._nmeResizeBuffer(this.allocated = B["int"](Math.max(a, 2 * this.allocated))) : this.allocated > a && this._nmeResizeBuffer(this.allocated = a);
            return this.length = a
        },
        __class__: Aa
    };
    var q = function() {};
    h["org.ascrypt.AES"] = q;
    q.__name__ = !0;
    q.encrypt = function(a, b, c, d) {
        null == c && (c = "ecb");
        q.check(a, b);
        a = a.slice();
        b = b.slice();
        q.init();
        q.ek(a);
        switch (c.toLowerCase()) {
            case "ecb":
                return Dc.core(a,
                    b, 16, q.ie);
            case "cbc":
                return lc.encrypt(a, b, 16, q.ie, d.slice());
            case "ctr":
                return hb.encrypt(a, b, 16, q.ie, d.slice());
            case "none":
                return q.ie(a, b);
            default:
                throw new z(q.ERROR_MODE);
        }
    };
    q.decrypt = function(a, b, c, d) {
        null == c && (c = "ecb");
        q.check(a, b);
        a = a.slice();
        b = b.slice();
        q.init();
        q.ek(a);
        switch (c.toLowerCase()) {
            case "ecb":
                return Dc.core(a, b, 16, q.id);
            case "cbc":
                return lc.decrypt(a, b, 16, q.id, d.slice());
            case "ctr":
                return hb.decrypt(a, b, 16, q.ie, d.slice());
            case "none":
                return q.id(a, b);
            default:
                throw new z(q.ERROR_MODE);
        }
    };
    q.init = function() {
        q.isrtab = [];
        q.isbox = [];
        q.xtime = [];
        for (var a = 0; 256 > a;) {
            var b = a++;
            q.isbox[q.sbox[b]] = b
        }
        for (a = 0; 16 > a;) b = a++, q.isrtab[q.srtab[b]] = b;
        for (a = 0; 128 > a;) b = a++, q.xtime[b] = b << 1, q.xtime[128 + b] = b << 1 ^ 27
    };
    q.sb = function(a, b) {
        for (var c = 0; 16 > c;) {
            var d = c++;
            a[d] = b[a[d]]
        }
    };
    q.ark = function(a, b) {
        for (var c = 0; 16 > c;) {
            var d = c++;
            a[d] ^= b[d]
        }
    };
    q.sr = function(a, b) {
        for (var c = a.slice(), d = 0; 16 > d;) {
            var e = d++;
            a[e] = c[b[e]]
        }
    };
    q.ek = function(a) {
        var b = a.length,
            c = 0,
            d = 1;
        switch (b) {
            case 16:
                c = 176;
                break;
            case 24:
                c = 208;
                break;
            case 32:
                c =
                    240
        }
        for (var e = b; e < c;) {
            var f = a.slice(e - 4, e);
            0 == e % b ? (f = [q.sbox[f[1]] ^ d, q.sbox[f[2]], q.sbox[f[3]], q.sbox[f[0]]], 256 <= (d <<= 1) && (d ^= 283)) : 24 < b && 16 == e % b && (f = [q.sbox[f[0]], q.sbox[f[1]], q.sbox[f[2]], q.sbox[f[3]]]);
            for (var g = 0; 4 > g;) a[e + g] = a[e + g - b] ^ f[g], g++;
            e += 4
        }
    };
    q.ie = function(a, b) {
        b = b.slice();
        var c = 16,
            d = a.length;
        for (q.ark(b, a.slice(0, 16)); c < d - 16;) q.sb(b, q.sbox), q.sr(b, q.srtab), q.mc(b), q.ark(b, a.slice(c, c + 16)), c += 16;
        q.sb(b, q.sbox);
        q.sr(b, q.srtab);
        q.ark(b, a.slice(c, c + 16));
        return b
    };
    q.id = function(a, b) {
        b = b.slice();
        var c = a.length,
            d = c - 32;
        q.ark(b, a.slice(c - 16, c));
        q.sr(b, q.isrtab);
        for (q.sb(b, q.isbox); 16 <= d;) q.ark(b, a.slice(d, d + 16)), q.mci(b), q.sr(b, q.isrtab), q.sb(b, q.isbox), d -= 16;
        q.ark(b, a.slice(0, 16));
        return b
    };
    q.mc = function(a) {
        for (var b = 0; 16 > b;) {
            var c = a[b],
                d = a[b + 1],
                e = a[b + 2],
                f = a[b + 3],
                g = c ^ d ^ e ^ f;
            a[b] = a[b] ^ g ^ q.xtime[c ^ d];
            a[b + 1] = a[b + 1] ^ g ^ q.xtime[d ^ e];
            a[b + 2] = a[b + 2] ^ g ^ q.xtime[e ^ f];
            a[b + 3] = a[b + 3] ^ g ^ q.xtime[f ^ c];
            b += 4
        }
    };
    q.mci = function(a) {
        for (var b = 0; 16 > b;) {
            var c = a[b],
                d = a[b + 1],
                e = a[b + 2],
                f = a[b + 3],
                g = c ^ d ^ e ^ f,
                m = q.xtime[g],
                h = q.xtime[q.xtime[m ^
                    c ^ e]] ^ g;
            g ^= q.xtime[q.xtime[m ^ d ^ f]];
            a[b] = a[b] ^ h ^ q.xtime[c ^ d];
            a[b + 1] = a[b + 1] ^ g ^ q.xtime[d ^ e];
            a[b + 2] = a[b + 2] ^ h ^ q.xtime[e ^ f];
            a[b + 3] = a[b + 3] ^ g ^ q.xtime[f ^ c];
            b += 4
        }
    };
    q.check = function(a, b) {
        a = a.length;
        if (16 != a && 24 != a && 32 != a) throw new z(q.ERROR_KEY);
        if (0 != b.length % 16) throw new z(q.ERROR_BLOCK);
    };
    var lc = function() {};
    h["org.ascrypt.utilities.CBC"] = lc;
    lc.__name__ = !0;
    lc.encrypt = function(a, b, c, d, e) {
        for (var f = [], g = b.length, h = 0; h < g;) {
            for (var k = 0; k < c;) {
                var l = k++;
                b[h + l] ^= e[l]
            }
            f = f.concat(d(a, b.slice(h, h + c)));
            e = f.slice(h,
                h + c);
            h += c
        }
        return f
    };
    lc.decrypt = function(a, b, c, d, e) {
        for (var f = b.length, g, h = [], k = 0; k < f;) {
            g = b.slice(k, k + c);
            h = h.concat(d(a, g));
            for (var l = 0; l < c;) {
                var n = l++;
                h[k + n] ^= e[n]
            }
            e = g.slice(0, c);
            k += c
        }
        return h
    };
    var hb = function() {};
    h["org.ascrypt.utilities.CTR"] = hb;
    hb.__name__ = !0;
    hb.encrypt = function(a, b, c, d, e) {
        return hb.core(a, b, c, d, e)
    };
    hb.decrypt = function(a, b, c, d, e) {
        return hb.core(a, b, c, d, e)
    };
    hb.core = function(a, b, c, d, e) {
        var f = b.length;
        e = e.slice();
        for (var g = 0; g < f;) {
            var h = d(a, e);
            for (var k = 0; k < c;) {
                var l = k++;
                b[g + l] ^=
                    h[l]
            }
            for (h = c - 1; 0 <= h && (--h, e[h]++, 0 == e[h]););
            g += c
        }
        return b
    };
    var Dc = function() {};
    h["org.ascrypt.utilities.ECB"] = Dc;
    Dc.__name__ = !0;
    Dc.core = function(a, b, c, d) {
        for (var e = [], f = b.length, g = 0; g < f;) e = e.concat(d(a, b.slice(g, g + c))), g += c;
        return e
    };
    var S = function() {
        this.reset()
    };
    h["terra.Buff"] = S;
    S.__name__ = !0;
    S.getMaxTime = function() {
        return 269 <= I._main.player.invVersion ? 1999999980 : 108E4
    };
    S.setup = function() {
        S.buffName = mc.$name;
        S.buffTip = mc.tip;
        S.COUNT = S.buffName.length;
        I.BUFFS = S.COUNT
    };
    S.$name = function(a) {
        return 0 <=
            a && a < S.COUNT ? S.buffName[a] : "?"
    };
    S.ttip = function(a) {
        return 0 <= a && a < S.COUNT ? S.buffTip[a] : "?"
    };
    S.prototype = {
        set: function(a, b) {
            this.id = a;
            this.time = b
        },
        reset: function() {
            this.time = this.id = 0
        },
        getTime: function() {
            var a = this.time / 60;
            if (0 > a) return "?";
            if (100 > a) return "" + (a | 0) + "s";
            a /= 60;
            if (100 > a) return "" + (a | 0) + "m";
            a /= 60;
            return 999 < a ? "*" : "" + (a | 0) + "h"
        },
        __class__: S
    };
    var A = function() {
        this.iconX = this.iconY = 0;
        this.text = "";
        this.stack = 0;
        this.code = "";
        this.id = 0;
        this.name = this.enName = ""
    };
    h["terra.Item"] = A;
    A.__name__ = !0;
    A.fromId =
        function(a) {
            var b = A.idMap.h[a];
            null == b && (b = new A, b.name = "Unknown", b.id = a, A.idMap.h[a] = b);
            return b
        };
    A.fromCode = function(a) {
        var b = A.codeMap.get(a);
        null == b && (b = new A, b.name = a, b.code = a, A.codeMap.set(a, b));
        return b
    };
    A.isEmpty = function(a) {
        return null == a || 0 == a.id && "" == a.name
    };
    A.isItem = function(a) {
        return null != a && (0 != a.id || "" != a.name)
    };
    A.init = function() {
        A.unknownIcon = new ca(40, 40, !0, 0);
        var a = new bb,
            b = a.graphics;
        b.beginFill(10088055);
        b.drawCircle(20, 20, 11.7);
        A.unknownIcon.draw(a);
        Mc.run();
        A.prefixes = wa.init();
        a = 0;
        for (b = A.list; a < b.length;) {
            var c = b[a];
            ++a;
            c.nameLq = c.name.toLowerCase();
            c.textLq = c.text.toLowerCase()
        }
    };
    A.count = function(a) {
        return 99999 < a ? "+?" : 0 > a ? "-?" : "" + a
    };
    A.prototype = {
        cropFrom: function(a, b, c) {
            this.iconX = b;
            this.iconY = c
        },
        loadMeta: function(a) {
            this.metadata = a;
            var b = a.indexOf("|");
            this.metatype = 0 <= b ? a.substring(0, b) : a;
            this.meta = Xa.parse(this, a);
            this.stack = this.meta.stack;
            this.text = this.meta.toString()
        },
        updateLang: function() {
            this.name = l.iloc("ItemName", this.pid, this.enName);
            this.nameLq = this.name.toLowerCase();
            this.text = this.meta.toString()
        },
        __class__: A
    };
    var Mc = function() {};
    h["terra.ItemParser"] = Mc;
    Mc.__name__ = !0;
    Mc.run = function() {
        for (var a = za.minId, b = za.count, c = 0; c < b;) {
            var d = new A,
                e = za.$name[c],
                f = a++;
            0 != f && "" == e && (e = "Unknown");
            var g = 0 != f ? "Vanilla:" + e : "";
            var h = za.pid[c];
            "" == h && (h = D.replace(e, " ", ""));
            d.pid = h;
            A.id2pid.h[f] = h;
            h;
            A.pid2id.set(h, f);
            f;
            d.id = f;
            d.name = e;
            d.enName = e;
            d.code = g;
            d.loadMeta(za.meta[c]);
            0 > f ? d.cropFrom(null, 0, -40) : d.cropFrom(null, 40 * (f & 31), 40 * (f >> 5));
            A.idMap.h[f] = d;
            A.codeMap.set(g, d);
            A.list.push(d);
            c++
        }
        I.NITEMS = za.minId - 1;
        I.ITEMS = za.maxId + 1
    };
    var sa = function(a, b, c) {
        this.kind = a;
        this.key = b;
        this.text = this.enText = c
    };
    h["terra.ItemPrefixPairDef"] = sa;
    sa.__name__ = !0;
    sa.prototype = {
        updateLang: function() {
            this.text = l.loc("meta.prefix", this.key, this.enText)
        },
        __class__: sa
    };
    var wa = function() {
        this.pairs = []
    };
    h["terra.ItemPrefix"] = wa;
    wa.__name__ = !0;
    wa.init = function() {
        for (var a = Nc.get(), b = new Ba, c = 0, d = a.length; c < d;) {
            var e = c++,
                f = a[e].split("|"),
                g = new wa;
            g.name = f.shift();
            g.enName = g.name;
            g.tier = B.parseInt(f.shift());
            for (var h = 0; h < f.length;) {
                var k = f[h];
                ++h;
                g.pairs.push({
                    kind: k.charAt(0),
                    val: y.substr(k, 1, null)
                })
            }
            g.updateText();
            wa.list.push(g);
            b.h[e] = g
        }
        return b
    };
    wa.prototype = {
        updateText: function() {
            for (var a = [], b = 0, c = this.pairs; b < c.length;) {
                var d = c[b];
                ++b;
                var e = wa.pairMap.get(d.kind);
                null != e && (l.isDebug ? a.push(e.text + ("(" + d.val + ")")) : a.push(D.replace(e.text, "$1", d.val)))
            }
            this.text = a.join("\n")
        },
        updateLang: function() {
            this.name = "None" == this.enName ? l.loc("meta.prefix", "noPrefix", "None") : l.iloc("Prefix", this.enName,
                this.enName);
            this.updateText()
        },
        __class__: wa
    };
    var Nc = function() {};
    h["terra.ItemPrefixData"] = Nc;
    Nc.__name__ = !0;
    Nc.get = function() {
        return "None|0|;Large|1|Z+12;Massive|1|Z+18;Dangerous|1|D+6|C+2|Z+5;Savage|2|D+9|Z+10|K+10;Sharp|1|D+16;Pointy|1|D+9;Tiny|-1|Z-18;Terrible|-2|D-16|Z-13|K-15;Small|-1|Z-10;Dull|-1|D-16;Unhappy|-2|S-8|Z-10|K-10;Bulky|1|D-6|S-17|Z+10|K+10;Shameful|-2|D-9|Z+10|K-20;Heavy|0|S-8|K+15;Light|0|S+17|K-10;Sighted|1|D+9|C+3;Rapid|2|S+17;Hasty|2|S+8;Intimidating|2|K+15;Deadly|2|D+9|S+3|C+2|K+5;Staunch|2|D+9|K+15;Awful|-2|D-16|K-10;Lethargic|-2|S-16|K-10;Awkward|-2|S-8|K-20;Powerful|1|D+16|S-8|C+1;Mystic|2|D+10|M-14;Adept|1|M-14;Masterful|2|D+15|M-14|K+5;Inept|-1|M+7;Ignorant|-2|D-10|M+21;Deranged|-1|D-10|K-10;Intense|-1|D+10|M+15;Taboo|1|S+10|M+10|K+10;Celestial|1|D+10|S-10|M-10|K+10;Furious|1|D+15|M+20|K+15;Keen|1|C+3;Superior|2|D+11|C+3|K+10;Forceful|1|K+15;Broken|-2|D-31|K-20;Damaged|-1|D-14;Shoddy|-2|D-9|K-15;Quick|1|S+8;Deadly|2|D+9|S+8;Agile|1|S+8|C+3;Nimble|1|S+4;Murderous|2|D+6|S+4|C+3;Slow|-1|S-17;Sluggish|-2|S-21;Lazy|-1|S-8;Annoying|-2|D-19|S-17;Nasty|1|D+6|S+8|C+2|K-10;Manic|1|D-10|S+10|M-10;Hurtful|1|D+9;Strong|1|K+15;Unpleasant|2|D+6|K+15;Weak|-1|K-20;Ruthless|1|D+19|K-10;Frenzying|0|D-16|S+17;Godly|2|D+16|C+5|K+15;Demonic|2|D+16|C+5;Zealous|1|C+5;Hard|2|d+1;Guarding|2|d+2;Armored|2|d+3;Warding|null|d+4;Arcane|null|m+20;Precise|null|C+2;Lucky|null|C+4;Jagged|null|D+1;Spiked|null|D+2;Angry|null|D+3;Menacing|null|D+4;Brisk|null|s+1;Fleeting|null|s+2;Hasty|null|s+3;Quick|null|s+4;Wild|null|Q+1;Rash|null|Q+2;Intrepid|null|Q+3;Violent|null|Q+4;Legendary|null|D+16|S+8|C+5|K+15|Z+10;Unreal|null|D+16|S+8|C+5|K+15;Mythical|null|D+15|S+14|C+5|M-20|K+15;Legendary (yoyo)|2|D+17|C+8|K+17;Fabled|2|D+15|p+10|t+3|K+15;Loyal|2|D+10|p+5|t+3|K+5;Worthy|2|D+15|p+8;Focused|1|D+10|t+3;Patient|null|D-5|t+3;Rabid|null|D+10|K-10;Ill-Tempered|1|D-5|p+10;Petty|-2|D-30;Feeble|-2|K-25;Skittish|-2|D-15|K-10;Eager|2|p+25;Ballistic|1|t+1;Scraggling|2|K+25".split(";")
    };
    var ma = function() {
        this.creativePowersOrder = [];
        this.creativePowers = ma.creativePowers_init();
        this.trail = null;
        this.superCartByte = 0;
        this.researchSlots = [];
        this.researchByItemPID = [];
        this.respawnTimer = this.lastTimeSaved_1 = this.lastTimeSaved_2 = this.golferScore = this.researchMysteryByte = 0;
        this.isDead = !1;
        this.bartenderQuests = this.voidVaultByte = 0;
        this.builderAccStatus = [];
        this.dpadBindings = [0, 0, 0, 0];
        this.playTimeChanged = !1;
        this.taxMoney = this.pveDeaths = this.pvpDeaths = this.metaVersion = this.metaFlags1 = this.metaFlags2 =
            this.playTimeL = this.playTimeH = this.playTimeSeconds = this.playTimeTicks = 0;
        this.finishedDD2Event = !1;
        this.extraUsingFlags = function(a) {
            a = [];
            for (var b = 0; 7 > b;) b++, a.push(!1);
            return a
        }(this);
        this.extraAccessory = this.unlockedBiomeTorches = this.usingBiomeTorches = !1;
        this.fishingQuestsCompleted = this.hideVisual = this.hideMisc = 0;
        this.hotbarLocked = !1;
        this.servers = [];
        this.buffs = function(a) {
            a = [];
            for (var b = 0; 44 > b;) b++, a.push(new S);
            return a
        }(this);
        this.researchDummies = ja.createSlots(40, function(a, c) {
            c.multi = !0;
            c.avail =
                function(a) {
                    return 200 <= a.invVersion
                }
        });
        this.tempItems = ja.createSlots(4, function(a, c) {
            c.multi = !0;
            c.avail = function(a) {
                return 200 <= a.invVersion
            }
        });
        this.voidItems = ja.createSlots(40, function(a, c) {
            c.multi = !0;
            c.favFlagMinVersion = 269;
            c.avail = function(a) {
                return 200 <= a.invVersion
            }
        });
        this.forgeItems = ja.createSlots(40, function(a, c) {
            c.multi = !0;
            c.avail = function(a) {
                return 184 <= a.invVersion
            }
        });
        this.safeItems = ja.createSlots(40, ma.__bankOrSafe_iter);
        this.bankItems = ja.createSlots(40, ma.__bankOrSafe_iter);
        this.equipmentDyes =
            ja.createSlots(5, ma.__equipment_iter);
        this.equipmentItems = ja.createSlots(5, ma.__equipment_iter);
        this.ammo = ja.createSlots(4, ma.__coinOrAmmo_iter);
        this.coins = ja.createSlots(4, ma.__coinOrAmmo_iter);
        this.inventory = ja.createSlots(50, function(a, c) {
            c.multi = !0;
            40 <= a && (c.avail = function(a) {
                return 58 <= a.invVersion
            });
            c.favFlagMinVersion = 145
        });
        this.currentLoadout = 0;
        this.loadouts = function(a) {
            a = [];
            for (var b = 0; 4 > b;) {
                var d = b++;
                a.push(new P(d))
            }
            return a
        }(this);
        this.shoesColor = 10512700;
        this.pantsColor = 16770735;
        this.underColor =
            10532055;
        this.shirtColor = 11511180;
        this.eyeColor = 6904395;
        this.skinColor = 16743770;
        this.hairDye = this.team = 0;
        this.hairColor = 14113335;
        this.hairStyle = 0;
        this.manaNow = this.manaMax = 20;
        this.healthNow = this.healthMax = 100;
        this.gender = 1;
        this.difficulty = 0;
        this.guid = "f2a63086-cfb5-41c8-bae4-712777bbf934";
        this.name = "Player";
        this.invVersion = 315;
        this.isSwitch = !1;
        this.version = 315;
        var a;
        this.hideInfo = [];
        for (a = 0; 13 > a;) this.hideInfo[a] = !1, a++;
        for (a = 0; 11 > a;) this.builderAccStatus.push(0), a++;
        this.builderAccStatus[0] = 1
    };
    h["terra.Player"] =
        ma;
    ma.__name__ = !0;
    ma.__coinOrAmmo_iter = function(a, b) {
        b.multi = !0;
        b.favFlagMinVersion = 145
    };
    ma.__equipment_iter = function(a, b) {
        b.avail = function(a) {
            return 145 <= a.invVersion
        }
    };
    ma.__bankOrSafe_iter = function(a, b) {
        b.multi = !0;
        20 <= a && (b.avail = function(a) {
            return 58 <= a.invVersion
        })
    };
    ma.creativePowers_init = function() {
        var a = [],
            b = !1;
        a.push(function(a, c) {
            c ? a.writeByte(b ? 1 : 0) : b = 0 != a.data.getUint8(a.position++)
        });
        a.push(function(a) {
            return function(a, b) {}
        }(this));
        a.push(function(a) {
            return function(a, b) {}
        }(this));
        a.push(function(a) {
            return function(a,
                b) {}
        }(this));
        a.push(function(a) {
            return function(a, b) {}
        }(this));
        var c = !1;
        a.push(function(a, b) {
            b ? a.writeByte(c ? 1 : 0) : c = 0 != a.data.getUint8(a.position++)
        });
        a.push(function(a) {
            return function(a, b) {}
        }(this));
        a.push(function(a) {
            return function(a, b) {}
        }(this));
        var d = 1;
        a.push(function(a, b) {
            b ? a.writeFloat(d) : d = a.readFloat()
        });
        var e = !1;
        a.push(function(a, b) {
            b ? a.writeByte(e ? 1 : 0) : e = 0 != a.data.getUint8(a.position++)
        });
        var f = !1;
        a.push(function(a, b) {
            b ? a.writeByte(f ? 1 : 0) : f = 0 != a.data.getUint8(a.position++)
        });
        var g = !1;
        a.push(function(a, b) {
            b ? a.writeByte(g ? 1 : 0) : g = 0 != a.data.getUint8(a.position++)
        });
        var h = 1;
        a.push(function(a, b) {
            b ? a.writeFloat(h) : h = a.readFloat()
        });
        var k = !1;
        a.push(function(a, b) {
            b ? a.writeByte(k ? 1 : 0) : k = 0 != a.data.getUint8(a.position++)
        });
        var l = 1;
        a.push(function(a, b) {
            b ? a.writeFloat(l) : l = a.readFloat()
        });
        return a
    };
    ma.prototype = {
        set_version: function(a) {
            this.version = a;
            this.invVersion = (this.isSwitch = 1E3 <= a) ? 1003 <= a ? 190 : 145 : a;
            return a
        },
        handle: function(a, b) {
            var c = this;
            if (b) {
                var d = this.loadouts[1 + this.currentLoadout];
                this.loadouts[0].setTo(d);
                d.clear()
            }
            a.littleEndian = !0;
            "littleEndian";
            var e;
            b ? (d = e = this.version, a.writeInt(d)) : this.set_version(e = a.readInt());
            d = this.isSwitch;
            e = this.invVersion;
            var f = ja.getMaxIds(e);
            na.maxId = f.item;
            if (145 <= e)
                if (b) a.writeUnsignedInt(1869374834), a.writeUnsignedInt(56846695), a.writeUnsignedInt(this.metaVersion), a.writeUnsignedInt(this.metaFlags1), a.writeUnsignedInt(this.metaFlags2), d && H.writeSharpString(a, this.guid);
                else {
                    f = a.readUnsignedInt();
                    var g = a.readUnsignedInt();
                    if (1869374834 !=
                        f || 56846695 != g) throw new z("That doesn't seem to be a valid profile.");
                    this.metaVersion = a.readUnsignedInt();
                    this.metaFlags1 = a.readUnsignedInt();
                    this.metaFlags2 = a.readUnsignedInt();
                    d && (this.guid = H.readSharpString(a))
                } b ? H.writeSharpString(a, this.name) : this.name = H.readSharpString(a);
            b ? a.writeByte(this.difficulty) : this.difficulty = a.data.getUint8(a.position++);
            145 <= e && (b ? (this.playTimeChanged && (f = this.playTimeSeconds, this.playTimeH = f / 429.4967295 | 0, this.playTimeL = this.playTimeTicks + (f % 429.4967295 * 1E7 | 0)),
                a.writeUnsignedInt(this.playTimeL), a.writeUnsignedInt(this.playTimeH)) : (this.playTimeL = a.readUnsignedInt(), this.playTimeH = a.readUnsignedInt(), this.playTimeTicks = this.playTimeL % 1E7, this.playTimeSeconds = (this.playTimeL / 1E7 | 0) + 429.4967295 * this.playTimeH | 0, this.playTimeChanged = !1));
            b ? a.writeInt(this.hairStyle) : this.hairStyle = a.readInt();
            82 <= e && (b ? a.writeByte(this.hairDye) : this.hairDye = a.data.getUint8(a.position++));
            315 <= e && (b ? a.writeByte(this.team) : this.team = a.data.getUint8(a.position++));
            83 <= e && (b ? (a.writeByte(this.hideVisual &
                255), 145 <= e && a.writeByte(this.hideVisual >> 8 & 255)) : (this.hideVisual = a.data.getUint8(a.position++), 145 <= e && (this.hideVisual |= a.data.getUint8(a.position++) << 8)));
            145 <= e && (b ? a.writeByte(this.hideMisc) : this.hideMisc = a.data.getUint8(a.position++));
            145 <= e ? b ? a.writeByte(this.gender) : this.gender = a.data.getUint8(a.position++) : b ? a.writeByte(4 > this.gender ? 1 : 0) : 0 != a.data.getUint8(a.position++) ? this.gender = 0 : this.gender = 4;
            b ? a.writeInt(this.healthNow) : this.healthNow = a.readInt();
            b ? a.writeInt(this.healthMax) : this.healthMax =
                a.readInt();
            b ? a.writeInt(this.manaNow) : this.manaNow = a.readInt();
            b ? a.writeInt(this.manaMax) : this.manaMax = a.readInt();
            if (145 <= e) {
                b ? a.writeByte(this.extraAccessory ? 1 : 0) : this.extraAccessory = 0 != a.data.getUint8(a.position++);
                230 <= e && (b ? a.writeByte(this.unlockedBiomeTorches ? 1 : 0) : this.unlockedBiomeTorches = 0 != a.data.getUint8(a.position++), b ? a.writeByte(this.usingBiomeTorches ? 1 : 0) : this.usingBiomeTorches = 0 != a.data.getUint8(a.position++));
                if (269 <= e) {
                    if (b) a.writeByte(this.extraUsingFlags[0] ? 1 : 0);
                    else this.extraUsingFlags[0] = 0 != a.data.getUint8(a.position++);
                    324 <= e && (b ? a.writeByte(0) : a.data.getUint8(a.position++));
                    if (b)
                        for (f = 1; 7 > f;) g = f++, a.writeByte(this.extraUsingFlags[g] ? 1 : 0);
                    else
                        for (f = 1; 7 > f;) g = f++, this.extraUsingFlags[g] = 0 != a.data.getUint8(a.position++)
                }
                if (d ? 190 < e : 184 <= e) b ? a.writeByte(this.finishedDD2Event ? 1 : 0) : this.finishedDD2Event = 0 != a.data.getUint8(a.position++);
                b ? a.writeInt(this.taxMoney) : this.taxMoney = a.readInt();
                269 <= e && (b ? (a.writeInt(this.pveDeaths), a.writeInt(this.pvpDeaths)) : (this.pveDeaths = a.readInt(), this.pvpDeaths = a.readInt()))
            }
            b ? H.writeColor(a, this.hairColor) : this.hairColor = a.data.getUint8(a.position++) << 16 | a.data.getUint8(a.position++) << 8 | a.data.getUint8(a.position++);
            b ? H.writeColor(a, this.skinColor) : this.skinColor = a.data.getUint8(a.position++) << 16 | a.data.getUint8(a.position++) << 8 | a.data.getUint8(a.position++);
            b ? H.writeColor(a, this.eyeColor) : this.eyeColor = a.data.getUint8(a.position++) << 16 | a.data.getUint8(a.position++) << 8 | a.data.getUint8(a.position++);
            b ? H.writeColor(a, this.shirtColor) : this.shirtColor = a.data.getUint8(a.position++) << 16 | a.data.getUint8(a.position++) << 8 | a.data.getUint8(a.position++);
            b ? H.writeColor(a, this.underColor) : this.underColor = a.data.getUint8(a.position++) <<
                16 | a.data.getUint8(a.position++) << 8 | a.data.getUint8(a.position++);
            b ? H.writeColor(a, this.pantsColor) : this.pantsColor = a.data.getUint8(a.position++) << 16 | a.data.getUint8(a.position++) << 8 | a.data.getUint8(a.position++);
            b ? H.writeColor(a, this.shoesColor) : this.shoesColor = a.data.getUint8(a.position++) << 16 | a.data.getUint8(a.position++) << 8 | a.data.getUint8(a.position++);
            var h = function(d) {
                d.handle(c, a, e, b)
            };
            f = function(a) {
                for (var b = 0; b < a.length;) {
                    var c = a[b];
                    ++b;
                    h(c)
                }
            };
            this.loadouts[0].handle(this, a, e, b);
            f(this.inventory);
            f(this.coins);
            f(this.ammo);
            for (g = 0; 5 > g;) {
                var k = g++;
                h(this.equipmentItems[k]);
                h(this.equipmentDyes[k])
            }
            if (168 <= e || d) f(this.bankItems), f(this.safeItems);
            else
                for (g = 0, k = this.bankItems.length; g < k;) {
                    var l = g++;
                    h(this.bankItems[l]);
                    h(this.safeItems[l])
                }
            f(this.forgeItems);
            f(this.voidItems);
            200 <= e && (b ? a.writeByte(this.voidVaultByte) : this.voidVaultByte = a.data.getUint8(a.position++));
            k = 269 <= e ? 44 : 77 <= e ? 22 : 10;
            for (f = 0; f < k;) b ? (a.writeInt(this.buffs[f].id), a.writeInt(this.buffs[f].time | 0)) : (g = a.readInt(), this.buffs[f].set(g,
                a.readInt())), f++;
            if (b) {
                k = this.servers.length;
                for (f = 0; f < k;) {
                    l = this.servers[f];
                    if (0 != l.spawnX || 0 != l.spawnY || 0 != l.address) a.writeInt(l.spawnX), a.writeInt(l.spawnY), a.writeInt(l.address), H.writeSharpString(a, l.name);
                    f++
                }
                a.writeInt(-1)
            } else
                for (f = 0; 200 > f;) {
                    g = a.readInt();
                    if (-1 == g) break;
                    l = new ac;
                    l.spawnX = g;
                    l.spawnY = a.readInt();
                    l.address = a.readInt();
                    l.name = H.readSharpString(a);
                    0 == l.spawnX && 0 == l.spawnY && 0 == l.address || this.servers.push(l);
                    f++
                }
            b ? a.writeByte(this.hotbarLocked ? 1 : 0) : this.hotbarLocked = 0 != a.data.getUint8(a.position++);
            if (145 <= e)
                for (k = 13, f = 0; f < k;) b ? a.writeByte(this.hideInfo[f] ? 1 : 0) : this.hideInfo[f] = 0 != a.data.getUint8(a.position++), f++;
            98 <= e && (b ? a.writeInt(this.fishingQuestsCompleted) : this.fishingQuestsCompleted = a.readInt());
            if (168 <= e) {
                if (!d || 190 < e)
                    for (k = 4, f = 0; f < k;) b ? a.writeInt(this.dpadBindings[f]) : this.dpadBindings[f] = a.readInt(), f++;
                k = 10;
                200 <= e && (k = 11);
                230 <= e && (k = 12);
                for (f = 0; f < k;) b ? a.writeInt(this.builderAccStatus[f]) : this.builderAccStatus[f] = a.readInt(), f++
            }
            184 <= e && (b ? a.writeInt(this.bartenderQuests) : this.bartenderQuests =
                a.readInt());
            if (200 <= e) {
                if (b) a.writeByte(this.isDead ? 1 : 0), this.isDead && a.writeInt(this.respawnTimer);
                else if (this.isDead = 0 != a.data.getUint8(a.position++)) this.respawnTimer = a.readInt();
                b ? (a.writeInt(this.lastTimeSaved_1), a.writeInt(this.lastTimeSaved_2), a.writeInt(this.golferScore)) : (this.lastTimeSaved_1 = a.readInt(), this.lastTimeSaved_2 = a.readInt(), this.golferScore = a.readInt());
                if (b) {
                    315 <= e && a.writeByte(this.researchMysteryByte);
                    this.researchByItemPID = [];
                    d = 0;
                    for (f = this.researchSlots; d < f.length;) g =
                        f[d], ++d, null != g.item && 0 != g.item.id && (k = A.id2pid.h[g.item.id], null != k && this.researchByItemPID.push(new nc(k, g.count)));
                    a.writeInt(this.researchByItemPID.length);
                    d = 0;
                    for (f = this.researchByItemPID; d < f.length;) g = f[d], ++d, H.writeSharpString(a, g.pid), a.writeInt(g.count)
                } else
                    for (this.researchMysteryByte = 315 <= e ? a.data.getUint8(a.position++) : 0, d = a.readInt(), this.researchByItemPID = [], this.researchSlots = [], f = 0; f < d;) k = H.readSharpString(a), g = a.readInt(), this.researchByItemPID.push(new nc(k, g)), k = A.pid2id.get(k),
                        null != k && (l = new na, l.item = A.idMap.h[k], l.count = g, l.multi = !0, l.favFlagMinVersion = 0, this.researchSlots.push(l)), f++;
                if (b) {
                    f = d = 0;
                    for (g = this.tempItems.length; f < g;) k = f++, this.tempItems[k].get_isValid() && (d |= 1 << k);
                    a.writeByte(d);
                    f = 0;
                    for (g = this.tempItems.length; f < g;) k = f++, 0 != (d & 1 << k) && this.tempItems[k].save(a)
                } else
                    for (d = a.data.getUint8(a.position++), f = 0, g = this.tempItems.length; f < g;) k = f++, 0 != (d & 1 << k) && this.tempItems[k].load(a)
            } else if (!b) {
                this.researchByItemPID = [];
                d = 0;
                for (f = this.tempItems; d < f.length;) g =
                    f[d], ++d, g.clear();
                this.researchSlots = []
            }
            if (220 <= e)
                if (b) {
                    d = 0;
                    for (f = this.creativePowersOrder; d < f.length;) g = f[d], ++d, a.writeByte(1), a.writeShort(g), this.creativePowers[g](a, b);
                    a.writeByte(0)
                } else
                    for (this.creativePowersOrder = []; 0 != a.data.getUint8(a.position++);) d = a.readShort(), this.creativePowersOrder.push(d), this.creativePowers[d](a, b);
            253 <= e ? b ? a.writeByte(this.superCartByte) : this.superCartByte = a.data.getUint8(a.position++) : b || (this.superCartByte = 0);
            if (269 <= e)
                for (b ? a.writeInt(this.currentLoadout) :
                    this.currentLoadout = a.readInt(), d = 1, f = this.loadouts.length; d < f;) g = d++, this.loadouts[g].handle(this, a, e, b);
            else b || (this.currentLoadout = 0);
            d = a.length;
            if (!b) {
                g = a.position;
                a.position = d - 1;
                k = a.data.getUint8(a.position++);
                if (0 < k && 16 > k) {
                    a.position = d - k;
                    for (f = 0; f < k && a.data.getUint8(a.position++) == k;) f++;
                    f >= k && (d -= k)
                }
                a.position = g
            }
            if (b) {
                if (null != this.trail)
                    for (this.trail.position = 0, k = this.trail.length; 0 <= --k;) d = this.trail.readByte(), a.writeByte(d)
            } else if (k = d - a.position, 0 < k)
                for (this.trail = new Aa; 0 <= --k;) this.trail.writeByte(a.data.getUint8(a.position++));
            else this.trail = null;
            if (b)
                for (f = k = 16 - (a.length & 15); 0 <= --k;) a.writeByte(f);
            this.loadouts[1 + this.currentLoadout].setTo(this.loadouts[0])
        },
        load: function(a) {
            this.handle(a, !1)
        },
        save: function(a) {
            this.handle(a, !0)
        },
        __class__: ma
    };
    var P = function(a) {
        this.hide = function(a) {
            a = [];
            for (var b = 0; 10 > b;) b++, a.push(!1);
            return a
        }(this);
        var b = this;
        this.primary = 0 == a;
        var c = 1 < a;
        this.items = ja.createSlots(10, function(a, e) {
            e.multi = !b.primary;
            e.favFlagMinVersion = 322;
            c && (e.visAvail = P.altLoadoutAvail);
            8 == a && (e.visAvail = c ? P.extraAccessoryAvailAlt : P.extraAccessoryAvail);
            9 == a && (e.visAvail = c ? P.masterAcessoryOrDyeAvailAlt : P.masterAcessoryOrDyeAvail)
        });
        this.social = ja.createSlots(10, function(a, e) {
            e.multi = !b.primary;
            e.favFlagMinVersion = 322;
            c && (e.visAvail = P.altLoadoutAvail);
            3 <= a && 8 > a && (e.visAvail = c ? P.socialAccessoryOrDyeAvailAlt : P.socialAccessoryOrDyeAvail);
            8 == a && (e.visAvail = c ? P.extraAccessoryAvailAlt : P.extraAccessoryAvail);
            9 == a && (e.visAvail = c ? P.masterAcessoryOrDyeAvailAlt : P.masterAcessoryOrDyeAvail)
        });
        this.dyes = ja.createSlots(10, function(a, e) {
            e.multi = !b.primary;
            e.favFlagMinVersion = 322;
            c && (e.visAvail = P.altLoadoutAvail);
            3 <= a && 8 > a && (e.visAvail = c ? P.socialAccessoryOrDyeAvailAlt : P.socialAccessoryOrDyeAvail);
            8 == a && (e.visAvail = c ? P.extraAccessoryAvailAlt : P.extraAccessoryAvail);
            9 == a && (e.visAvail = c ? P.masterAcessoryOrDyeAvailAlt : P.masterAcessoryOrDyeAvail)
        })
    };
    h["terra.PlayerLoadout"] = P;
    P.__name__ = !0;
    P.extraAccessoryAvail = function(a) {
        return 145 <= a.invVersion && a.extraAccessory || 3 == a.difficulty
    };
    P.socialAccessoryOrDyeAvail = function(a) {
        return 81 <= a.invVersion
    };
    P.masterAcessoryOrDyeAvail = function(a) {
        return 200 <= a.invVersion && 3 !=
            a.difficulty
    };
    P.extraAccessoryAvailAlt = function(a) {
        return 269 <= a.invVersion && P.extraAccessoryAvail(a)
    };
    P.socialAccessoryOrDyeAvailAlt = function(a) {
        return 269 <= a.invVersion && P.socialAccessoryOrDyeAvail(a)
    };
    P.masterAcessoryOrDyeAvailAlt = function(a) {
        return 269 <= a.invVersion && P.masterAcessoryOrDyeAvail(a)
    };
    P.altLoadoutAvail = function(a) {
        return 269 <= a.invVersion
    };
    P.prototype = {
        handle: function(a, b, c, d) {
            if (this.primary)
                if (145 <= c) {
                    for (var e = 0; 10 > e;) {
                        var f = e++;
                        this.items[f].handle(a, b, c, d)
                    }
                    for (e = 0; 10 > e;) f = e++,
                        this.social[f].handle(a, b, c, d);
                    for (e = 0; 10 > e;) f = e++, this.dyes[f].handle(a, b, c, d)
                } else if (81 <= c) {
                for (e = 0; 8 > e;) f = e++, this.items[f].handle(a, b, c, d);
                for (e = 0; 8 > e;) f = e++, this.social[f].handle(a, b, c, d);
                for (e = 0; 8 > e;) f = e++, this.dyes[f].handle(a, b, c, d)
            } else {
                for (e = 0; 8 > e;) f = e++, this.items[f].handle(a, b, c, d);
                for (e = 0; 3 > e;) f = e++, this.social[f].handle(a, b, c, d);
                if (39 < c)
                    for (e = 0; 3 > e;) f = e++, this.dyes[f].handle(a, b, c, d)
            } else {
                for (e = 0; 10 > e;) f = e++, this.items[f].handle(a, b, c, d);
                for (e = 0; 10 > e;) f = e++, this.social[f].handle(a,
                    b, c, d);
                for (e = 0; 10 > e;) f = e++, this.dyes[f].handle(a, b, c, d);
                for (a = 0; 10 > a;) c = a++, d ? b.writeByte(this.hide[c] ? 1 : 0) : this.hide[c] = 0 != b.data.getUint8(b.position++)
            }
        },
        clear: function() {
            for (var a = 0; 10 > a;) {
                var b = a++;
                this.items[b].clear();
                this.social[b].clear();
                this.dyes[b].clear()
            }
        },
        setTo: function(a) {
            for (var b = 0; 10 > b;) {
                var c = b++;
                this.items[c].setTo(a.items[c]);
                this.social[c].setTo(a.social[c]);
                this.dyes[c].setTo(a.dyes[c])
            }
        },
        __class__: P
    };
    var nc = function(a, b) {
        this.pid = a;
        this.count = b
    };
    h["terra.PlayerResearch"] = nc;
    nc.__name__ = !0;
    nc.prototype = {
        __class__: nc
    };
    var ja = function() {};
    h["terra.PlayerTools"] = ja;
    ja.__name__ = !0;
    ja.createSlots = function(a, b) {
        for (var c = [], d = 0; d < a;) {
            var e = d++,
                f = new na;
            c[e] = f;
            null != b && b(e, f)
        }
        return c
    };
    ja.getMaxIds = function(a) {
        if (200 < a) {
            a = 16384;
            var b = 1024
        } else 190 <= a ? (a = 3929, b = 205) : 184 <= a ? (a = 3883, b = 205) : 175 <= a ? (a = 3796, b = 190) : 168 <= a ? (a = 3729, b = 190) : 145 <= a ? (a = 3601, b = 190) : 98 <= a ? (a = 2748, b = 139) : 93 <= a ? (a = 2288, b = 103) : 77 <= a ? (a = 1965, b = 93) : 70 <= a ? (a = 1725, b = 80) : 69 <= a ? (a = 1614, b = 80) : (a = 603, b = 40);
        return {
            item: a,
            buff: b
        }
    };
    var ac = function() {
        this.name = "";
        this.spawnX = this.spawnY = this.address = 0
    };
    h["terra.ServerEntry"] = ac;
    ac.__name__ = !0;
    ac.prototype = {
        __class__: ac
    };
    var na = function() {
        this.isFavorited = !1;
        this.favFlagMinVersion = 0;
        this.avail = na._default_avail;
        this.multi = !1;
        this.count = this.prefix = 0;
        this.item = null;
        var a = this;
        this.visAvail = function(b) {
            return a.avail(b)
        }
    };
    h["terra.Slot"] = na;
    na.__name__ = !0;
    na._default_avail = function(a) {
        return !0
    };
    na.prototype = {
        setTo: function(a) {
            this.item = a.item;
            this.count = this.multi ? a.count :
                0 < a.count ? 1 : 0;
            this.prefix = a.prefix
        },
        get_isValid: function() {
            return null != this.item && 0 != this.item.id
        },
        clear: function() {
            this.item = null;
            this.count = this.prefix = 0
        },
        isEmpty: function() {
            return A.isEmpty(this.item)
        },
        handle: function(a, b, c, d) {
            this.avail(a) ? (a = d ? null != this.item ? this.item.id : 0 : b.readInt(), a > na.maxId && (a = 0), d ? b.writeInt(a) : this.item = A.fromId(a), d ? this.multi && b.writeInt(this.count) : this.count = this.multi ? b.readInt() : 1, d ? b.writeByte(this.prefix) : this.prefix = b.data.getUint8(b.position++), 0 < this.favFlagMinVersion &&
                c >= this.favFlagMinVersion && (d ? b.writeByte(this.isFavorited ? 1 : 0) : this.isFavorited = 0 != b.data.getUint8(b.position++))) : d || this.clear()
        },
        load: function(a) {
            this.item = A.fromId(a.readInt());
            this.count = this.multi ? a.readInt() : 1;
            this.prefix = a.data.getUint8(a.position++)
        },
        save: function(a) {
            a.writeInt(null != this.item ? this.item.id : 0);
            this.multi && a.writeInt(this.count);
            a.writeByte(this.prefix)
        },
        __class__: na
    };
    var mc = function() {};
    h["terra.data.TdBuff"] = mc;
    mc.__name__ = !0;
    var za = function() {};
    h["terra.data.TdItem"] = za;
    za.__name__ = !0;
    var M = function() {};
    h["terra.data.TiPrefix"] = M;
    M.__name__ = !0;
    var Da = function(a, b, c, d, e, f, g) {
        this.unknown = 0;
        var h = 1 < d,
            k;
        this.size = a;
        this.base = c;
        this.perStyle = d;
        this.lineHeight = b;
        this.pages = [];
        b = e.length;
        for (a = -1; ++a < b;) {
            var l = e[a];
            this.pages[a] = k = [];
            for (c = -1; ++c < d;) k.push(Q.getBitmapData(l[c]))
        }
        this.glyphs = new Ba;
        d = h ? 9 : 8;
        a = 0;
        for (b = f.length; a < b;) e = new Ib(f[a + 1], f[a + 2], f[a + 3], f[a + 4], f[a + 5], f[a + 6], f[a + 7], h ? f[a + 8] : 0), this.glyphs.h[f[a]] = e, a += d;
        a = 0;
        for (b = g.length; a < b;) f = this.glyphs.h[g[a]],
            null != f && (null == f.k && (f.k = new Ba), f.k.h[g[a + 1]] = g[a + 2]), a += 3
    };
    h["utils.BMFont"] = Da;
    Da.__name__ = !0;
    Da.prototype = {
        measure: function(a, b, c, d, e, f) {
            null == e && (e = 0);
            null == d && (d = 0);
            null == f && (f = new Xb);
            f.text = a;
            a += "\n";
            f.x = b;
            f.halign = d;
            f.y = c;
            f.valign = e;
            var g = -1,
                h = 0,
                k = a.length,
                l, n, p, q = this.glyphs,
                r = null,
                t, u = this.lineHeight,
                w = this.unknown;
            for (f.width = f.height = n = p = 0; ++g < k;)
                if (10 == (l = a.charCodeAt(g))) f.sizes[h++] = n, n > f.width && (f.width = n), n = 0, p += u, r = null;
                else {
                    var v;
                    !(v = null != (t = q.h[l])) && (v = 0 < w) && (t = l = w, v = null !=
                        (t = q.h[t]));
                    v && (null != r && r.h.hasOwnProperty(l) && (n += r.h[l]), n += t.s, r = t.k)
                } f.height = p;
            f.left = b - (f.width * d >> 1);
            f.top = c - (f.height * e >> 1);
            f.right = f.left + f.width;
            f.bottom = f.top + f.height;
            return f
        },
        draw: function(a, b, c, d, e, f, g, h) {
            null == g && (g = 0);
            null == f && (f = 0);
            null == e && (e = 0);
            null == h && (h = Da._inf = this.measure(b, c, d, e, f, Da._inf));
            var k = -1,
                l = 0,
                m = b.length,
                n, p, q = this.glyphs,
                r = null,
                t = this.lineHeight,
                v = this.pages[g],
                w = this.unknown,
                u = Da._rect,
                y = Da._offset;
            g = c - (h.sizes[0] * e >> 1);
            for (d -= h.height * f >> 1; ++k < m;) 10 == (n = b.charCodeAt(k)) ?
                (g = c - (h.sizes[++l] * e >> 1), d += t, r = null) : (!(p = null != (f = q.h[n])) && (p = 0 < w) && (f = n = w, p = null != (f = q.h[f])), p && (null != r && r.h.hasOwnProperty(n) && (g += r.h[n]), u.width = f.w, u.height = f.h, n = g + f.x, p = d + f.y, u.x = n, y.tx = n - f.u, u.y = p, y.ty = p - f.v, a.draw(v[f.p], y, null, null, u, !1), g += f.s))
        },
        select: function(a, b, c, d, e, f, g, h, k) {
            null == g && (g = 0);
            null == f && (f = 0);
            null == k && (k = Da._inf = this.measure(a, d, e, f, g, Da._inf));
            var l = 0,
                m = a.length,
                n, p = this.glyphs,
                q = this.lineHeight,
                r = this.unknown;
            if (b > c) {
                var t = b;
                b = c;
                c = t
            }
            0 > b && (b = 0);
            c > m && (c = m);
            var u =
                n = d - (k.sizes[0] * f >> 1);
            e -= k.height * g >> 1;
            if (0 == c) h(u, n, e);
            else
                for (t = -1; ++t < m;)
                    if (10 == (g = a.charCodeAt(t))) h(u, n, e), u = n = d - (k.sizes[++l] * f >> 1), e += q;
                    else {
                        var x;
                        !(x = null != (g = p.h[g])) && (x = 0 < r) && (x = null != (g = p.h[r]));
                        if (x && (t == b && (u = n), n += g.s, t + 1 == c)) {
                            h(b == c ? n : u, n, e);
                            break
                        }
                    }
        },
        __class__: Da
    };
    var Ib = function(a, b, c, d, e, f, g, h) {
        this.u = a;
        this.v = b;
        this.w = c;
        this.h = d;
        this.x = e;
        this.y = f;
        this.s = g;
        this.p = h;
        this.k = null
    };
    h["utils.BMGlyph"] = Ib;
    Ib.__name__ = !0;
    Ib.prototype = {
        __class__: Ib
    };
    var Xb = function() {
        this.sizes = []
    };
    h["utils.BMInfo"] =
        Xb;
    Xb.__name__ = !0;
    Xb.prototype = {
        __class__: Xb
    };
    var H = function() {};
    h["utils.ByteArrayTools"] = H;
    H.__name__ = !0;
    H.readSharpString = function(a) {
        var b = a.data.getUint8(a.position++);
        return 0 != b ? a.readUTFBytes(b) : ""
    };
    H._getUTFBytesCount = function(a) {
        for (var b = 0, c = -1, d = a.length, e; ++c < d;) e = a.charCodeAt(c), b = 127 >= e ? b + 1 : 2047 >= e ? b + 2 : 65535 >= e ? b + 3 : b + 4;
        return b
    };
    H.writeSharpString = function(a, b) {
        a.writeByte(H._getUTFBytesCount(b));
        a.writeUTFBytes(b)
    };
    H.writeColor = function(a, b) {
        a.writeByte(b >> 16 & 255);
        a.writeByte(b >> 8 &
            255);
        a.writeByte(b & 255)
    };
    H.__init_crypto = function() {
        (function(a) {
            var b = !0;
            try {
                var c = window.document;
                c = c[function(a) {
                    a = "";
                    for (var b = 0, c; 8 > b;) c = b++, c = y.cca("ajdhqlhm", c), a += va(c & -16 | (c & 15) + 10 * b + 1 & 15);
                    return a
                }(a)];
                var d = c[function(a) {
                    a = "";
                    for (var b = 0, c; 4 > b;) c = b++, c = y.cca("`zmn", c), a += va(c & -16 | (c & 15) + 16 * b + 8 & 15);
                    return a
                }(a)];
                a = "";
                c = 0;
                for (var e; 10 > c;) {
                    var f = c++;
                    e = y.cca('2)+wan"ik)', f);
                    a += va(e & -16 | (e & 15) + 14 * c + 10 & 15)
                }
                var g = a;
                var h = d.indexOf(g);
                if (0 > h) {
                    g = "";
                    e = 0;
                    for (var k; 27 > e;) {
                        var l = e++;
                        k = y.cca(">-'{abli{onvaxlcjc&kpih(mm'",
                            l);
                        g += va(k & -16 | (k & 15) + 6 * e + 6 & 15)
                    }
                    h = d.indexOf(g)
                }
                if (0 > h) {
                    k = "";
                    l = 0;
                    for (var n; 43 > l;) {
                        var p = l++;
                        n = y.cca('=*"nbh`jaod\u007fd~wjulj`!bbjjghlsdv)fj`*l\u007ffclj"', p);
                        k += va(n & -16 | (n & 15) + 8 * l + 5 & 15)
                    }
                    h = d.indexOf(k)
                }
                b = 0 <= h && 8 > h
            } catch (N) {
                N instanceof z && (N = N.val)
            }
            return b
        })(this) ? H.aesKey = ab.utf2ints("h3y_gUyZ"): H.aesKey = []
    };
    H.encrypt = function(a) {
        null == H.aesKey && H.__init_crypto();
        H._ints = ab.byteArray2ints(a, H._ints);
        ab.ints2byteArray(q.encrypt(H.aesKey, H._ints, "cbc", H.aesKey), a);
        return a
    };
    H.decrypt = function(a) {
        null ==
            H.aesKey && H.__init_crypto();
        H._ints = ab.byteArray2ints(a, H._ints);
        ab.ints2byteArray(q.decrypt(H.aesKey, H._ints, "cbc", H.aesKey), a);
        return a
    };
    var ab = function() {};
    h["utils.Convert"] = ab;
    ab.__name__ = !0;
    ab.utf2ints = function(a, b) {
        null == b ? b = [] : b.splice(0, b.length);
        for (var c = -1, d = a.length, e; ++c < d;) e = y.cca(a, c), b.push(e & 255), b.push(e >> 8 & 255);
        return b
    };
    ab.byteArray2ints = function(a, b) {
        null == b ? b = [] : b.splice(0, b.length);
        var c = a.position,
            d = a.length;
        for (a.position = 0; 0 < d--;) b.push(a.data.getUint8(a.position++));
        a.position =
            c;
        return b
    };
    ab.ints2byteArray = function(a, b) {
        null == b ? b = new Aa : b.clear();
        for (var c = -1, d = a.length; ++c < d;) b.writeByte(a[c]);
        b.position = 0;
        return b
    };
    var Oc = function() {};
    h["utils.Mix"] = Oc;
    Oc.__name__ = !0;
    Oc.__js_init__ = function() {
        for (var a = [5, 17, 14, 12, -2, 7, 0, 17, -2, 14, 3, 4], b = "", c = 0, d; null != (d = a[c++]);) b = 0 > d ? b + (10 + -d).toString(36).toUpperCase() : b + (10 + d).toString(36).toLowerCase();
        va = J.getClass("")[b]
    };
    var qc = function(a, b) {
        this.ready = null;
        this.entryMap = new Y;
        this.entries = [];
        var c = this,
            d = new XMLHttpRequest;
        d.open("GET",
            a, !0);
        d.responseType = "arraybuffer";
        d.onload = function(a) {
            a = d.response;
            if (null != a) {
                a = ra.ofData(a);
                a = new gc(a);
                a = Bb.readZip(a).h;
                for (var e; null != a;) {
                    e = a[0];
                    a = a[1];
                    var g = new Ec(e);
                    c.entries.push(g);
                    c.entryMap.set(e.fileName, g);
                    g
                }
                c.ready = !0
            } else c.ready = !1;
            null != b && b(c)
        };
        d.onerror = function(a) {
            c.ready = !1;
            null != b && b(c)
        };
        d.send()
    };
    h["utils.PakoZip"] = qc;
    qc.__name__ = !0;
    qc.prototype = {
        getText: function(a) {
            a = this.entryMap.get(a);
            return null != a ? a.getText() : null
        },
        getJSON: function(a) {
            a = this.getText(a);
            return null !=
                a ? JSON.parse(a) : null
        },
        __class__: qc
    };
    var Ec = function(a) {
        this.text = null;
        this.hasText = !1;
        this.bytes = null;
        this.hasBytes = !1;
        this.entry = a
    };
    h["utils.PakoEntry"] = Ec;
    Ec.__name__ = !0;
    Ec.prototype = {
        getBytes: function() {
            if (!this.hasBytes)
                if (this.hasBytes = !0, this.entry.compressed) {
                    var a = this.entry.data.b.bufferValue,
                        b = oa.field(window, "pako");
                    a = oa.field(b, "inflateRaw")(a);
                    this.bytes = ra.ofData(a)
                } else this.bytes = this.entry.data;
            return this.bytes
        },
        getText: function() {
            if (!this.hasText) {
                this.hasText = !0;
                var a = this.getBytes();
                this.text = null != a ? a.toString() : null
            }
            return this.text
        },
        __class__: Ec
    };
    var cb = function() {};
    h["utils.WebArgs"] = cb;
    cb.__name__ = !0;
    cb.init = function() {
        var a = document.location.href,
            b;
        if (0 <= (b = a.indexOf("?"))) {
            a = y.substr(a, b + 1, null);
            var c = a.split("&");
            for (b = c.length; 0 <= --b;) {
                var d = c[b];
                a = d.indexOf("=");
                if (0 <= a) {
                    var e = y.substr(d, a + 1, null);
                    d = y.substr(d, 0, a);
                    e = unescape(e);
                    cb.map.set(d, e);
                    e
                } else cb.map.set(d, ""), ""
            }
        }
    };
    var $c = 0;
    Array.prototype.indexOf && (y.indexOf = function(a, b, c) {
        return Array.prototype.indexOf.call(a,
            b, c)
    });
    h.Math = Math;
    String.prototype.__class__ = h.String = String;
    String.__name__ = !0;
    h.Array = Array;
    Array.__name__ = !0;
    Date.prototype.__class__ = h.Date = Date;
    Date.__name__ = ["Date"];
    var ad = h.Int = {
            __name__: ["Int"]
        },
        bd = h.Dynamic = {
            __name__: ["Dynamic"]
        },
        Tc = h.Float = Number;
    Tc.__name__ = ["Float"];
    var Uc = h.Bool = Boolean;
    Uc.__ename__ = ["Bool"];
    var cd = h.Class = {
            __name__: ["Class"]
        },
        dd = {};
    null == Array.prototype.map && (Array.prototype.map = function(a) {
        for (var b = [], c = 0, d = this.length; c < d;) {
            var e = c++;
            b[e] = a(this[e])
        }
        return b
    });
    Eb.content = [];
    var Jc = {},
        Ic = Pc.ArrayBuffer || Pa;
    null == Ic.prototype.slice && (Ic.prototype.slice = Pa.sliceImpl);
    var Yc = Pc.DataView || xc,
        dc = Pc.Uint8Array || $a._new;
    n.__init();
    (function() {
        var a = Event.prototype,
            b = Z.prototype;
        a.clone = b.clone;
        a.isDefaultPrevented = b.isDefaultPrevented;
        a.get_target = b.get_target;
        a.set_target = b.set_target;
        a.get_currentTarget = b.get_currentTarget;
        a.set_currentTarget = b.set_currentTarget
    })();
    var va;
    Oc.__js_init__();
    t.AssetNames = "img/overlay.png img/buffs.png img/color.png img/nitems.png img/og.png img/shadow.png img/side.png img/visual.png".split(" ");
    t.AssetBytes = [5647, 210278, 48411, 12147, 142332, 120777, 18384, 21898];
    I.NITEMS = -49;
    I.hack = function() {
        var a = ca.prototype,
            b = a.nmeLoadFromFile;
        a.nmeLoadFromFile = function(a, d) {
            switch (a) {
                case "img/color.png":
                case "img/shadow.png":
                    a += "?v=2020-06-24"
            }
            Rc.log(a);
            b.call(this, a, d)
        };
        return !0
    }();
    l.defLang = l.initDefLang();
    l.isDebug = !1;
    l.noBMFont = !1;
    l.useCustomFont = !1;
    l.rxIVar = new Wa("\\{\\$(\\w+)\\.(\\w+)\\}", "g");
    la.maxId = 16384;
    O.maxId = 16384;
    u.useBMFont = !0;
    u.canvasFont = "17px sans-serif";
    u.canvasFontPre = "17px ";
    u.canvasFontSerif =
        "sans-serif";
    u.canvasLineHeight = 23;
    ha.rxPage = new Wa("^Page (\\d+)$", "");
    ha.rxPages = new Wa("^Pages (\\d+)\\+$", "");
    ha.rxAuto = new Wa("^(.+?)\\((\\d+)\\)$", "");
    ha.rxNum = new Wa("^\\d+\\-\\d+$", "");
    Sa.PART_GENDER = [3, 4, 5, 6, 7];
    Sa.ftColor = "Hair Skin Eyes Shirt Undershirt Pants Shoes".split(" ");
    qa.nullPoint = new ba(0, 0);
    db.goalPerItem = new Ba;
    Ha.removeTooltipEn = "Removes the spawn point for the world.\nYou'll spawn on the default location afterwards.";
    Ha.removeTooltip = Ha.removeTooltipEn;
    ya.versionNameMap = new Y;
    La.prefix = "/terrasavr/research";
    La.prefixLen = La.prefix.length;
    Ja.confirmText = "Confirm?";
    Ja.confirmTextEn = "Confirm?";
    ib.count = 0;
    Ma.i64tmp = new uc(0, 0);
    ka.LEN_EXTRA_BITS_TBL = [0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0, -1, -1];
    ka.LEN_BASE_VAL_TBL = [3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258];
    ka.DIST_EXTRA_BITS_TBL = [0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, -1, -1];
    ka.DIST_BASE_VAL_TBL = [1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97,
        129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577
    ];
    ka.CODE_LENGTHS_POS = [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15];
    J.__toStr = {}.toString;
    $a.BYTES_PER_ELEMENT = 1;
    Q.cache = new yc;
    Q.libraries = new Y;
    Q.initialized = !1;
    ia.pool = [];
    n.qTimeStamp = Date.now() + 0;
    q.ERROR_KEY = "Invalid key size. Key size needs to be either 128, 192 or 256 bits.\n";
    q.ERROR_MODE = "Invalid mode of operation. Supported modes are ECB, CBC, CTR or NONE.\n";
    q.ERROR_BLOCK = "Invalid block size. Block size is fixed at 128 bits.\n";
    q.srtab = [0, 5, 10, 15, 4, 9, 14, 3, 8, 13, 2, 7, 12, 1, 6, 11];
    q.sbox = [99, 124, 119, 123, 242, 107, 111, 197, 48, 1, 103, 43, 254, 215, 171, 118, 202, 130, 201, 125, 250, 89, 71, 240, 173, 212, 162, 175, 156, 164, 114, 192, 183, 253, 147, 38, 54, 63, 247, 204, 52, 165, 229, 241, 113, 216, 49, 21, 4, 199, 35, 195, 24, 150, 5, 154, 7, 18, 128, 226, 235, 39, 178, 117, 9, 131, 44, 26, 27, 110, 90, 160, 82, 59, 214, 179, 41, 227, 47, 132, 83, 209, 0, 237, 32, 252, 177, 91, 106, 203, 190, 57, 74, 76, 88, 207, 208, 239, 170, 251, 67, 77, 51, 133, 69, 249, 2, 127, 80, 60, 159, 168, 81, 163, 64, 143, 146, 157, 56, 245, 188, 182, 218, 33, 16,
        255, 243, 210, 205, 12, 19, 236, 95, 151, 68, 23, 196, 167, 126, 61, 100, 93, 25, 115, 96, 129, 79, 220, 34, 42, 144, 136, 70, 238, 184, 20, 222, 94, 11, 219, 224, 50, 58, 10, 73, 6, 36, 92, 194, 211, 172, 98, 145, 149, 228, 121, 231, 200, 55, 109, 141, 213, 78, 169, 108, 86, 244, 234, 101, 122, 174, 8, 186, 120, 37, 46, 28, 166, 180, 198, 232, 221, 116, 31, 75, 189, 139, 138, 112, 62, 181, 102, 72, 3, 246, 14, 97, 53, 87, 185, 134, 193, 29, 158, 225, 248, 152, 17, 105, 217, 142, 148, 155, 30, 135, 233, 206, 85, 40, 223, 140, 161, 137, 13, 191, 230, 66, 104, 65, 153, 45, 15, 176, 84, 187, 22
    ];
    A.idMap = new Ba;
    A.id2pid = new Ba;
    A.pid2id = new Y;
    A.codeMap = new Y;
    A.list = [];
    wa.pairDefs = [new sa("D", "damage", "$1% Damage"), new sa("d", "defense", "$1 Defense"), new sa("M", "manaCost", "$1% Mana Cost"), new sa("m", "mana", "$1 Max Mana"), new sa("C", "critChance", "$1% Critical strike chance"), new sa("K", "knockback", "$1% Knockback"), new sa("S", "speed", "$1% Speed"), new sa("Q", "meleeSpeed", "$1% Melee Speed"), new sa("s", "moveSpeed", "$1% Movement Speed"), new sa("Z", "size", "$1% Size"), new sa("t", "tagDamage", "$1 Summon Tag Damage"), new sa("p", "armorPen",
        "$1 Penentration")];
    wa.pairMap = function(a) {
        a = new Y;
        for (var b = 0, c = wa.pairDefs; b < c.length;) {
            var d = c[b];
            ++b;
            a.set(d.kind, d);
            d
        }
        return a
    }(this);
    wa.list = [];
    na.maxId = 16384;
    mc.$name = ";Obsidian Skin;Regeneration;Swiftness;Gills;Ironskin;Mana Regeneration;Magic Power;Featherfall;Spelunker;Invisibility;Shine;Night Owl;Battle;Thorns;Water Walking;Archery;Hunter;Gravitation;Shadow Orb;Poisoned;Potion Sickness;Darkness;Cursed;On Fire!;Tipsy;Well Fed;Fairy;Werewolf;Clairvoyance;Bleeding;Confused;Slow;Weak;Merfolk;Silenced;Broken Armor;Horrified;The Tongue;Cursed Inferno;Pet Bunny;Baby Penguin;Pet Turtle;Paladins Shield;Frostburn;Baby Eater;Chilled;Frozen;Honey;Pygmies;Baby Skeletron Head;Baby Hornet;Tiki Spirit;Pet Lizard;Pet Parrot;Baby Truffle;Pet Sapling;Wisp;Rapid Healing;Holy Protection;Leaf Crystal;Baby Dinosaur;Ice Barrier;Panic!;Baby Slime;Eyeball Spring;Baby Snowman;Burning;Suffocation;Ichor;Acid Venom;Weapon Imbue: Acid Venom;Midas;Weapon Imbue: Cursed Flames;Weapon Imbue: Fire;Weapon Imbue: Gold;Weapon Imbue: Ichor;Weapon Imbue: Nanites;Weapon Imbue: Confetti;Weapon Imbue: Poison;Blackout;Pet Spider;Squashling;Ravens;Black Cat;Cursed Sapling;Water Candle;Cozy Fire;Chaos State;Heart Lamp;Rudolph;Puppy;Baby Grinch;Ammo Box;Mana Sickness;Beetle Endurance;Beetle Endurance;Beetle Endurance;Beetle Might;Beetle Might;Beetle Might;Fairy;Fairy;Wet;Mining;Heartreach;Calm;Builder;Titan;Flipper;Summoning;Dangersense;Ammo Reservation;Lifeforce;Endurance;Rage;Inferno;Wrath;Minecart;Lovestruck;Stinky;Fishing;Sonar;Crate;Warmth;Hornet;Imp;Zephyr Fish;Bunny Mount;Pigron Mount;Slime Mount;Turtle Mount;Bee Mount;Spider;Twins;Pirate;Faun;Slime;BuffName.MinecartLegacyUnused;Sharknado;UFO;UFO Mount;Drill Mount;Scutlix Mount;Electrified;Moon Bite;Happy!;Banner;Feral Bite;Webbed;Bewitched;Life Drain;Magic Lantern;Shadowflame;Baby Face Monster;Crimson Heart;Stoned;Peace Candle;Star in a Bottle;Sharpened;Dazed;Deadly Sphere;Unicorn Mount;Obstructed;Distorted;Dryads Blessing;{$BuffName.Minecart};BuffName.MinecartMechLegacyUnused;Cute Fishron Mount;Penetrated;Solar Blaze;Solar Blaze;Solar Blaze;Life Nebula;Life Nebula;Life Nebula;Mana Nebula;Mana Nebula;Mana Nebula;Damage Nebula;Damage Nebula;Damage Nebula;Stardust Cell;Celled;{$BuffName.Minecart};BuffName.MinecartWoodLegacyUnused;Dryads Bane;Stardust Guardian;Stardust Dragon;Daybroken;Suspicious Looking Eye;Companion Cube;Sugar Rush;Basilisk Mount;Mighty Wind;Withered Armor;Withered Weapon;Oozed;Striking Moment;Creative Shock;Propeller Gato;Flickerwick;Hoardagron;Betsys Curse;Oiled;Ballista Panic!;Plenty Satisfied;Exquisitely Stuffed;{$BuffName.Minecart};BuffName.DesertMinecartLegacyUnused;{$BuffName.Minecart};BuffName.FishMinecartLegacyUnused;Golf Cart;Sanguine Bat;Vampire Frog;The Bast Defense;Baby Finch;Estee;Sugar Glider;Shark Pup;{$BuffName.Minecart};BuffName.BeeMinecartLegacyUnused;{$BuffName.Minecart};BuffName.LadybugMinecartLegacyUnused;{$BuffName.Minecart};BuffName.PigronMinecartLegacyUnused;{$BuffName.Minecart};BuffName.SunflowerMinecartLegacyUnused;{$BuffName.Minecart};BuffName.HellMinecartLegacyUnused;Witchs Broom;{$BuffName.Minecart};BuffName.ShroomMinecartLegacyUnused;{$BuffName.Minecart};BuffName.AmethystMinecartLegacyUnused;{$BuffName.Minecart};BuffName.TopazMinecartLegacyUnused;{$BuffName.Minecart};BuffName.SapphireMinecartLegacyUnused;{$BuffName.Minecart};BuffName.EmeraldMinecartLegacyUnused;{$BuffName.Minecart};BuffName.RubyMinecartLegacyUnused;{$BuffName.Minecart};BuffName.DiamondMinecartLegacyUnused;{$BuffName.Minecart};BuffName.AmberMinecartLegacyUnused;{$BuffName.Minecart};BuffName.BeetleMinecartLegacyUnused;{$BuffName.Minecart};BuffName.MeowmereMinecartLegacyUnused;{$BuffName.Minecart};BuffName.PartyMinecartLegacyUnused;{$BuffName.Minecart};BuffName.PirateMinecartLegacyUnused;{$BuffName.Minecart};BuffName.SteampunkMinecartLegacyUnused;Lucky;Lil Harpy;Fennec Fox;Glittery Butterfly;Baby Imp;Baby Red Panda;Desert Tiger;Plantero;Flamingo;Dynamite Kitten;Baby Werewolf;Shadow Mimic;{$BuffName.Minecart};BuffName.CoffinMinecartLegacyUnused;Enchanted Daggers;Digging Molecart;BuffName.DiggingMoleMinecartLegacyUnused;Volt Bunny;Painted Horse Mount;Majestic Horse Mount;Dark Horse Mount;Pogo Stick Mount;Pirate Ship Mount;Tree Mount;Santank Mount;Goat Mount;Book Mount;Slime Prince;Suspicious Eye;Eater of Worms;Spider Brain;Skeletron Jr.;Honey Bee;Destroyer-Lite;Rez and Spaz;Mini Prime;Plantera Seedling;Toy Golem;Tiny Fishron;Phantasmal Dragon;Moonling;Fairy Princess;Jack O Lantern;Everscream Sapling;Ice Queen;Alien Skater;Baby Ogre;Itsy Betsy;Lava Shark Mount;Titanium Barrier;BuffName.BlandWhipEnemyDebuff;Durendals Blessing;BuffName.SwordWhipNPCDebuff;BuffName.ScytheWhipEnemyDebuff;Harvest Time;A Nice Buff;BuffName.FlameWhipEnemyDebuff;Jungles Fury;BuffName.ThornWhipNPCDebuff;BuffName.RainbowWhipNPCDebuff;Slime Princess;Winged Slime Mount;BuffName.MaceWhipNPCDebuff;Sparkle Slime;Cerebral Mindtrick;Terraprisma;Hellfire;Frostbite;Flinx;BuffName.BoneWhipNPCDebuff;Bernie;Glommer;Tiny Deerclops;Pig;Chester;Peckish;Hungry;Starving;Abigail;Hearty Meal;BuffName.TentacleSpike;Fart Kart;BuffName.FartMinecartLegacyUnused;BuffName.CoolWhipNPCDebuff;Slime Royals;Blessing of the Moon;Biome Sight;Blood Butchered;Junimo;Terra Fart Kart;BuffName.TerraFartMinecartLegacyUnused;Strategist;Blue Chicken;Shadow Candle;Spiffo;Caveling Gardener;Shimmering;The Dirtiest Block;Mushroom Boi!;Swarm Biter;BuffName.CobWhipNPCDebuff;BuffName.CorruptWhipNPCDebuff;BuffName.CrimsonWhipNPCDebuff;BuffName.MeteorWhipNPCDebuff;BuffName.FlowerWhipNPCDebuff;BuffName.EelWhipNPCDebuff;BuffName.ConstellationWhipNPCDebuff;BuffName.MoonLordWhipNPCDebuff;Whip Spider;Alchemic Enhancement;BuffName.FlowerWhipNPCDebuffProc;BuffName.MoonLordWhipNPCDebuffProc;BuffName.MeteorWhipNPCDebuffProc;Instinct of the Raptor;Pufferfish;Cenaxe;Friendly Boulder;Nature of the Rat;Hemorrhage;Torch Blessing;Curse of the Bat;Blue Roller Skates;Green Roller Skates;Classic Roller Skates;Party Roller Skates;Friendly Rainbow Boulder;High Spirits;Form of the Fae;Cattiva;Foxparks;Chillet;Chillet Ignis".split(";");
    mc.tip = ";Immune to lava;Provides life regeneration;25% increased movement speed;Allows you to breathe in liquids;Increase defense by 8;Increased mana regeneration;20% increased magic damage;Press UP or DOWN to control speed of descent;Shows the location of treasure and ore;Grants invisibility;Emitting light;Increased night vision;Increased enemy spawn rate;Attackers also take damage;Press DOWN to enter water;10% increased bow damage and 20% increased arrow speed;Shows the location of enemies;Press UP to reverse gravity;A magical orb that provides light;Slowly losing life;Cannot consume anymore healing items;Decreased light vision;Cannot use any items;Slowly losing life;Increased melee abilities, lowered defense;Minor improvements to all stats;A fairy is following you;Physical abilities are increased;Magic powers are increased;Cannot regenerate life;Movement is reversed;Movement speed is reduced;Physical abilities are decreased;Can breathe and move easily underwater;Cannot use items that require mana;Defense is cut in half;You have seen something nasty, there is no escape.;You are being sucked into the mouth;Losing life;I think it wants your carrot;I think it wants your fish;Happy turtle time!;25% of damage taken will be redirected to another player;Its either really hot or really cold. Either way it REALLY hurts;A baby Eater of Souls is following you;Your movement speed has been reduced;You cant move!;Life regeneration is increased;The pygmies will fight for you;Dont even ask...;It thinks you are its mother;A friendly spirit is following you;Chillin like a reptilian;Polly wants the cracker;Isnt this just soooo cute?;A little sapling is following you;A wisp is following you;Life regeneration is greatly increased;You will dodge the next attack;Shoots crystal leaves at nearby enemies;A baby dinosaur is following you;Damage taken is reduced by 25%;Movement speed is increased;The baby slime will fight for you;An eyeball spring is following you;A baby snowman is following you;Losing life and slowed movement;Losing life;Reduced defense;Losing life;Melee attacks inflict acid venom on your targets;Drop more money on death;Melee attacks inflict enemies with cursed flames;Melee attacks set enemies on fire;Melee attacks make enemies drop more gold;Melee attacks decrease enemies defense;Melee attacks confuse enemies;Melee attacks cause confetti to appear;Melee attacks poison enemies;Light vision severely reduced;A spider is following you;A squashling is following you;The ravens will attack your enemies;A black kitty is following you;A cursed sapling is following you;Increased monster spawn rate;Life regen is slightly increased;Using the Rod of Discord will take life;Life regen is increased;Riding the red nosed reindeer;A puppy is following you;A baby grinch is following you;20% chance to save ammo;Magic damage reduced by ;Absorbs 15% of damage taken;Absorbs 30% of damage taken;Absorbs 45% of damage taken;Melee damage and speed increase by 10%;Melee damage and speed increase by 20%;Melee damage and speed increase by 30%;A fairy is following you;A fairy is following you;You are dripping water;25% increased mining speed;Increased heart pickup range;Decreased enemy spawn rate;Increased placement speed and range;Increased knockback;Move like normal in water;Increased your max number of minions by 1;You can see nearby hazards;20% chance to save ammo;20% increased max life;10% reduced damage;10% increased critical chance;Nearby enemies are ignited;10% increased damage;Riding in a minecart;You are in love!;You smell terrible;Increased fishing power;You can see whats biting your hook;Greater chance of fishing up a crate;Reduced damage from cold sources;The hornet will fight for you;The imp will fight for you;It likes swimming around you;You are craving carrots;Now you see me...;BOOOIIINNNG!;Slow if by land, zoom if by sea;BzzzBzzBZZZZBzzz;The spider will fight for you;The twins will fight for you;The pirate will fight for you;His name is Shaun;You are slimy and sticky;BuffDescription.MinecartLegacyUnused;The sharknado will fight for you;The UFO will fight for you;Its a good thing you had a MAC;Riding in a flying drill;Pew Pew;Moving hurts!;You are unable to absorb healing effects;Movement speed increased and monster spawns reduced;Increased damage and defense from the following:;Increased damage, Decreased life regen, Causes status effects;You are stuck;Increased your max number of minions by 1;Increased life regeneration;An enchanted lantern is lighting your way;Losing life;A baby face monster is following you;A magical heart that provides light;You are completely petrified!;Decreased monster spawn rate;Increased mana regeneration;Melee weapons have armor penetration;Movement is greatly slowed;The Deadly Sphere will fight for you;Charge ahead... fabulously!;You cant see!;Gravity around you is distorted;The power of nature protects you;{$BuffDescription.Minecart};BuffDescription.MinecartMechLegacyUnused;Just dont make it crawl.;Bleeding Out;Absorbs 20% of damage taken, repel enemies when taking damage;Absorbs 20% of damage taken, repel enemies when taking damage;Absorbs 20% of damage taken, repel enemies when taking damage;Increased life regeneration;Increased life regeneration;Increased life regeneration;Increased mana regeneration;Increased mana regeneration;Increased mana regeneration;15% increased damage;30% increased damage;45% increased damage;The stardust cell will fight for you;being eaten by cells;{$BuffDescription.Minecart};BuffDescription.MinecartWoodLegacyUnused;The power of nature compells you;The stardust guardian will protect you;The stardust dragon will protect you;Incinerated by solar rays;A suspicious looking eye that provides light;Will never threaten to stab you and, in fact, cannot speak;20% increased movement and mining speed;Crash into anyone... and EVERYONE!;The wind moves you around!;Your armor is lowered!;Your attacks are weaker!;Movement is significantly reduced;400% increased damage for next melee strike;You have lost the power of creation!;A propeller gato is following you;A flickerwick is following you;A hoardagron is following you;Defense is lowered;Taking more damage from being on fire;Your ballistas rapidly shoot in panic!;Medium improvements to all stats;Major improvements to all stats;{$BuffDescription.Minecart};BuffDescription.DesertMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.FishMinecartLegacyUnused;A fair way to cross the fairway;The sanguine bat will fight for you;The vampire frog will fight for you;Defense is increased by 5;The baby finch will fight for you;Estee is following you;A sugar glider is following you;Doo doo doo doo doo doo;{$BuffDescription.Minecart};BuffDescription.BeeMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.LadybugMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.PigronMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.SunflowerMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.HellMinecartLegacyUnused;It flies! WITCHCRAFT!;{$BuffDescription.Minecart};BuffDescription.ShroomMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.AmethystMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.TopazMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.SapphireMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.EmeraldMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.RubyMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.DiamondMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.AmberMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.BeetleMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.MeowmereMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.PartyMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.PirateMinecartLegacyUnused;{$BuffDescription.Minecart};BuffDescription.SteampunkMinecartLegacyUnused;You are feeling pretty lucky;Cuteness from above;What does the fox say? Better yet, what does the fox HEAR?!;Truly, truly outrageous;Just wait till his terrible twos!;A baby red panda is following you;The desert tiger will fight beside you;Little Plantero is following you;Flamingogogo;Not for use in cannons;A baby werewolf is following you;A shadow mimic is following you;{$BuffDescription.Minecart};BuffDescription.CoffinMinecartLegacyUnused;Death by a thousand cuts;The Molecart will dig for you;BuffDescription.DiggingMoleMinecartLegacyUnused;A volt bunny is ecstatic about you;Riding a Painted Horse;Riding a Majestic Horse;Riding a Dark Horse;Kss-shik! Kss-shik! Kss-shik!;Youre the captain now;Run, forest, run!;Crossing off the naughty list...;This ride is totally metal!;The Book is now helping in your guidance;He answers to a higher authority;Just keepin an eye out...;May ruin several backyards;Its crawling around... icky;Skeletron Jr. Is following you;A honey bee is following you;For destruction on the go;You have special eyes!;Each tool can commit murder;What exactly does it eat, anyway?;Got myself a crying, talking, sleeping, walking, living idol!;A sea-green marquess of the abyss;It keeps looking at the Moon;A friend from beyond;The light of the fair folk illuminates all;A small Jack O Lantern is fiendishly lighting the way;Taking the tree for a walk!;Ice Queen has been reborn as your companion;How do you do, fellow humans?;Hes got a big stick and he doesnt know how to use it;Itsy Betsy is following you;Surfing the molten seas!;Defensive shards surround you;BuffDescription.BlandWhipEnemyDebuff;Whip speed is increased;BuffDescription.SwordWhipNPCDebuff;BuffDescription.ScytheWhipEnemyDebuff;Whip speed is increased;Summons a snowflake to fight for you;BuffDescription.FlameWhipEnemyDebuff;Whip speed is increased;BuffDescription.ThornWhipNPCDebuff;BuffDescription.RainbowWhipNPCDebuff;She is the higher authority;BOING FLAP BOING!;BuffDescription.MaceWhipNPCDebuff;You are slimy and sparkly;Increased critical chance and minion damage;The Blades of the Empress will fight for you;Slowly losing life;Its either really hot or really cold. Either way it REALLY hurts;The snow flinx will fight for you;BuffDescription.BoneWhipNPCDebuff;Youre always there for me, Bernie;Its fuzzy! And slimy...;Holy crap!;Walking back bacon!;Otto von Chesterfield, Esquire;You could eat, but its not so bad.;You are quite hungry and feeling weak.;You are starving to death! Eat immediately!;Abigail will fight to protect you;Increased Life Regeneration;BuffDescription.TentacleSpike;{$BuffDescription.Minecart};BuffDescription.FartMinecartLegacyUnused;BuffDescription.CoolWhipNPCDebuff;The final authority, they will unite the kingdoms!;You turned into a wolf!;Shows the location of infected blocks;Bleeding out rapidly;Keeper of the Forest;{$BuffDescription.Minecart};BuffDescription.TerraFartMinecartLegacyUnused;Increased your max number of sentries by 1;The nametag says Shane;Dispels the peace of towns;This is how you died;Huk arrr gruk tu!;Youve gone insubstantial!;You can tell by all the extra dirt;Mushroom Boi will fight for you;Chomp chomp;BuffDescription.CobWhipNPCDebuff;BuffDescription.CorruptWhipNPCDebuff;BuffDescription.CrimsonWhipNPCDebuff;BuffDescription.MeteorWhipNPCDebuff;BuffDescription.FlowerWhipNPCDebuff;BuffDescription.EelWhipNPCDebuff;BuffDescription.ConstellationWhipNPCDebuff;BuffDescription.MoonLordWhipNPCDebuff;Summons a spider to fight for you;Fresh buffs last longer;BuffDescription.FlowerWhipNPCDebuffProc;BuffDescription.MoonLordWhipNPCDebuffProc;BuffDescription.MeteorWhipNPCDebuffProc;You turned into a velociraptor!;A friendly pufferfish is following you;Axing the real questions;A friendly boulder is rolling around with you;You turned into a rat!;Bleeding profusely;Nearby torches will be converted to match the biome;You turned into a bat!;This is how I roll;This is how I roll;This is how I roll;This is how I roll;A friendly rainbow boulder is rolling around with you;Slightly increased movement speed, mining speed, and placement speed;You turned into a pixie!;Cattiva will fight for you;Foxparks will fight for you;Chillet is exploring with you\nThe movement is quite intense!;Chillet Ignis is exploring with you\nBe careful of petting it too long, or sparks WILL start flying!".split(";");
    za.minId = 0;
    za.maxId = 6145;
    za.count = 6146;
    za.$name = ";Iron Pickaxe;Dirt Block;Stone Block;Iron Broadsword;Mushroom;Iron Shortsword;Iron Hammer;Torch;Wood;Iron Axe;Iron Ore;Copper Ore;Gold Ore;Silver Ore;Copper Watch;Silver Watch;Gold Watch;Depth Meter;Gold Bar;Copper Bar;Silver Bar;Iron Bar;Gel;Wooden Sword;Wooden Door;Stone Wall;Acorn;Lesser Healing Potion;Life Crystal;Dirt Wall;Bottle;Wooden Table;Furnace;Wooden Chair;Iron Anvil;Work Bench;Goggles;Lens;Wooden Bow;Wooden Arrow;Flaming Arrow;Shuriken;Suspicious Looking Eye;Demon Bow;War Axe of the Night;Lights Bane;Unholy Arrow;Chest;Band of Regeneration;Magic Mirror;Jesters Arrow;Angel Statue;Cloud in a Bottle;Hermes Boots;Enchanted Boomerang;Demonite Ore;Demonite Bar;Heart;Corrupt Seeds;Vile Mushroom;Ebonstone Block;Grass Seeds;Sunflower;Vilethorn;Starfury;Purification Powder;Vile Powder;Rotten Chunk;Worm Tooth;Worm Food;Copper Coin;Silver Coin;Gold Coin;Platinum Coin;Fallen Star;Copper Greaves;Iron Greaves;Silver Greaves;Gold Greaves;Copper Chainmail;Iron Chainmail;Silver Chainmail;Gold Chainmail;Grappling Hook;Chain;Shadow Scale;Piggy Bank;Mining Helmet;Copper Helmet;Iron Helmet;Silver Helmet;Gold Helmet;Wood Wall;Wood Platform;Flintlock Pistol;Musket;Musket Ball;Minishark;Iron Bow;Shadow Greaves;Shadow Scalemail;Shadow Helmet;Nightmare Pickaxe;The Breaker;Candle;Copper Chandelier;Silver Chandelier;Gold Chandelier;Mana Crystal;Lesser Mana Potion;Band of Starpower;Flower of Fire;Magic Missile;Dirt Rod;Shadow Orb;Meteorite;Meteorite Bar;Hook;Flamarang;Molten Fury;Volcano;Molten Pickaxe;Meteor Helmet;Meteor Suit;Meteor Leggings;Bottled Water;Space Gun;Rocket Boots;Gray Brick;Gray Brick Wall;Red Brick;Red Brick Wall;Clay Block;Blue Brick;Blue Brick Wall;Chain Lantern;Green Brick;Green Brick Wall;Pink Brick;Pink Brick Wall;Gold Brick;Gold Brick Wall;Silver Brick;Silver Brick Wall;Copper Brick;Copper Brick Wall;Spike;Water Candle;Book;Cobweb;Necro Helmet;Necro Breastplate;Necro Greaves;Bone;Muramasa;Cobalt Shield;Aqua Scepter;Lucky Horseshoe;Shiny Red Balloon;Harpoon;Spiky Ball;Ball O Hurt;Blue Moon;Handgun;Water Bolt;Bomb;Dynamite;Grenade;Sand Block;Glass;Sign;Ash Block;Obsidian;Hellstone;Hellstone Bar;Mud Block;Sapphire;Ruby;Emerald;Topaz;Amethyst;Diamond;Glowing Mushroom;Star;Ivy Whip;Breathing Reed;Flipper;Healing Potion;Mana Potion;Blade of Grass;Thorn Chakram;Obsidian Brick;Obsidian Skull;Mushroom Grass Seeds;Jungle Grass Seeds;Wooden Hammer;Star Cannon;Blue Phaseblade;Red Phaseblade;Green Phaseblade;Purple Phaseblade;White Phaseblade;Yellow Phaseblade;Meteor Hamaxe;Empty Bucket;Water Bucket;Lava Bucket;Jungle Rose;Stinger;Vine;Feral Claws;Anklet of the Wind;Staff of Regrowth;Hellstone Brick;Whoopie Cushion;Shackle;Molten Hamaxe;Flamelash;Phoenix Blaster;Sunfury;Hellforge;Clay Pot;Natures Gift;Bed;Silk;Restoration Potion;Restoration Potion;Jungle Hat;Jungle Shirt;Jungle Pants;Molten Helmet;Molten Breastplate;Molten Greaves;Meteor Shot;Sticky Bomb;Black Lens;Sunglasses;Wizard Hat;Top Hat;Tuxedo Shirt;Tuxedo Pants;Summer Hat;Bunny Hood;Plumbers Hat;Plumbers Shirt;Plumbers Pants;Heros Hat;Heros Shirt;Heros Pants;Fish Bowl;Archaeologists Hat;Archaeologists Jacket;Archaeologists Pants;Black Thread;Green Thread;Ninja Hood;Ninja Shirt;Ninja Pants;Leather;Red Hat;Goldfish;Robe;Robot Hat;Gold Crown;Hellfire Arrow;Sandgun;Guide Voodoo Doll;Diving Helmet;Familiar Shirt;Familiar Pants;Familiar Wig;Demon Scythe;Nights Edge;Dark Lance;Coral;Cactus;Trident;Silver Bullet;Throwing Knife;Spear;Blowpipe;Glowstick;Seed;Wooden Boomerang;Aglet;Sticky Glowstick;Poisoned Knife;Obsidian Skin Potion;Regeneration Potion;Swiftness Potion;Gills Potion;Ironskin Potion;Mana Regeneration Potion;Magic Power Potion;Featherfall Potion;Spelunker Potion;Invisibility Potion;Shine Potion;Night Owl Potion;Battle Potion;Thorns Potion;Water Walking Potion;Archery Potion;Hunter Potion;Gravitation Potion;Gold Chest;Daybloom Seeds;Moonglow Seeds;Blinkroot Seeds;Deathweed Seeds;Waterleaf Seeds;Fireblossom Seeds;Daybloom;Moonglow;Blinkroot;Deathweed;Waterleaf;Fireblossom;Shark Fin;Feather;Tombstone;Mime Mask;Antlion Mandible;Illegal Gun Parts;The Doctors Shirt;The Doctors Pants;Golden Key;Shadow Chest;Shadow Key;Obsidian Brick Wall;Jungle Spores;Loom;Piano;Dresser;Bench;Bathtub;Red Banner;Green Banner;Blue Banner;Yellow Banner;Lamp Post;Tiki Torch;Barrel;Chinese Lantern;Cooking Pot;Safe;Skull Lantern;Trash Can;Candelabra;Pink Vase;Mug;Keg;Ale;Bookcase;Throne;Bowl;Bowl of Soup;Toilet;Grandfather Clock;Armor Statue;Goblin Battle Standard;Tattered Cloth;Sawmill;Cobalt Ore;Mythril Ore;Adamantite Ore;Pwnhammer;Excalibur;Hallowed Seeds;Ebonsand Block;Cobalt Hat;Cobalt Helmet;Cobalt Mask;Cobalt Breastplate;Cobalt Leggings;Mythril Hood;Mythril Helmet;Mythril Hat;Mythril Chainmail;Mythril Greaves;Cobalt Bar;Mythril Bar;Cobalt Chainsaw;Mythril Chainsaw;Cobalt Drill;Mythril Drill;Adamantite Chainsaw;Adamantite Drill;Dao of Pow;Mythril Halberd;Adamantite Bar;Glass Wall;Compass;Diving Gear;GPS;Obsidian Horseshoe;Obsidian Shield;Tinkerers Workshop;Cloud in a Balloon;Adamantite Headgear;Adamantite Helmet;Adamantite Mask;Adamantite Breastplate;Adamantite Leggings;Spectre Boots;Adamantite Glaive;Toolbelt;Pearlsand Block;Pearlstone Block;Mining Shirt;Mining Pants;Pearlstone Brick;Iridescent Brick;Mudstone Brick;Cobalt Brick;Mythril Brick;Pearlstone Brick Wall;Iridescent Brick Wall;Mudstone Brick Wall;Cobalt Brick Wall;Mythril Brick Wall;Holy Water;Unholy Water;Silt Block;Fairy Bell;Breaker Blade;Blue Torch;Red Torch;Green Torch;Purple Torch;White Torch;Yellow Torch;Demon Torch;Clockwork Assault Rifle;Cobalt Repeater;Mythril Repeater;Dual Hook;Star Statue;Sword Statue;Slime Statue;Goblin Statue;Shield Statue;Bat Statue;Fish Statue;Bunny Statue;Skeleton Statue;Reaper Statue;Woman Statue;Imp Statue;Gargoyle Statue;Gloom Statue;Hornet Statue;Bomb Statue;Crab Statue;Hammer Statue;Potion Statue;Spear Statue;Cross Statue;Jellyfish Statue;Bow Statue;Boomerang Statue;Boot Statue;Chest Statue;Bird Statue;Axe Statue;Corrupt Statue;Tree Statue;Anvil Statue;Pickaxe Statue;Mushroom Statue;Eyeball Statue;Pillar Statue;Heart Statue;Pot Statue;Sunflower Statue;King Statue;Queen Statue;Piranha Statue;Planked Wall;Wooden Beam;Adamantite Repeater;Adamantite Sword;Cobalt Sword;Mythril Sword;Moon Charm;Ruler;Crystal Ball;Disco Ball;Sorcerer Emblem;Warrior Emblem;Ranger Emblem;Demon Wings;Angel Wings;Magical Harp;Rainbow Rod;Ice Rod;Neptunes Shell;Mannequin;Greater Healing Potion;Greater Mana Potion;Pixie Dust;Crystal Shard;Clown Hat;Clown Shirt;Clown Pants;Flamethrower;Bell;Harp;Red Wrench;Wire Cutter;Active Stone Block;Inactive Stone Block;Lever;Laser Rifle;Crystal Bullet;Holy Arrow;Magic Dagger;Crystal Storm;Cursed Flames;Soul of Light;Soul of Night;Cursed Flame;Cursed Torch;Adamantite Forge;Mythril Anvil;Unicorn Horn;Dark Shard;Light Shard;Red Pressure Plate;Wire;Spell Tome;Star Cloak;Megashark;Shotgun;Philosophers Stone;Titan Glove;Cobalt Naginata;Switch;Dart Trap;Boulder;Green Pressure Plate;Gray Pressure Plate;Brown Pressure Plate;Mechanical Eye;Cursed Arrow;Cursed Bullet;Soul of Fright;Soul of Might;Soul of Sight;Gungnir;Hallowed Plate Mail;Hallowed Greaves;Hallowed Helmet;Cross Necklace;Mana Flower;Mechanical Worm;Mechanical Skull;Hallowed Headgear;Hallowed Mask;Slime Crown;Light Disc;Music Box (Overworld Day);Music Box (Eerie);Music Box (Night);Music Box (Title);Music Box (Underground);Music Box (Boss 1);Music Box (Jungle);Music Box (Corruption);Music Box (Underground Corruption);Music Box (The Hallow);Music Box (Boss 2);Music Box (Underground Hallow);Music Box (Boss 3);Soul of Flight;Music Box;Demonite Brick;Hallowed Repeater;Drax;Explosives;Inlet Pump;Outlet Pump;1 Second Timer;3 Second Timer;5 Second Timer;Candy Cane Block;Candy Cane Wall;Santa Hat;Santa Shirt;Santa Pants;Green Candy Cane Block;Green Candy Cane Wall;Snow Block;Snow Brick;Snow Brick Wall;Blue Light;Red Light;Green Light;Blue Present;Green Present;Yellow Present;Snow Globe;Carrot;Adamantite Beam;Adamantite Beam Wall;Demonite Brick Wall;Sandstone Brick;Sandstone Brick Wall;Ebonstone Brick;Ebonstone Brick Wall;Red Stucco;Yellow Stucco;Green Stucco;Gray Stucco;Red Stucco Wall;Yellow Stucco Wall;Green Stucco Wall;Gray Stucco Wall;Ebonwood;Rich Mahogany;Pearlwood;Ebonwood Wall;Rich Mahogany Wall;Pearlwood Wall;Ebonwood Chest;Rich Mahogany Chest;Pearlwood Chest;Ebonwood Chair;Rich Mahogany Chair;Pearlwood Chair;Ebonwood Platform;Rich Mahogany Platform;Pearlwood Platform;Bone Platform;Ebonwood Work Bench;Rich Mahogany Work Bench;Pearlwood Work Bench;Ebonwood Table;Rich Mahogany Table;Pearlwood Table;Ebonwood Piano;Rich Mahogany Piano;Pearlwood Piano;Ebonwood Bed;Rich Mahogany Bed;Pearlwood Bed;Ebonwood Dresser;Rich Mahogany Dresser;Pearlwood Dresser;Ebonwood Door;Rich Mahogany Door;Pearlwood Door;Ebonwood Sword;Ebonwood Hammer;Ebonwood Bow;Rich Mahogany Sword;Rich Mahogany Hammer;Rich Mahogany Bow;Pearlwood Sword;Pearlwood Hammer;Pearlwood Bow;Rainbow Brick;Rainbow Brick Wall;Ice Block;Reds Wings;Reds Helmet;Reds Breastplate;Reds Leggings;Fish;Ice Boomerang;Keybrand;Cutlass;Boreal Wood Work Bench;True Excalibur;True Nights Edge;Frostbrand;Boreal Wood Table;Red Potion;Tactical Shotgun;Ivy Chest;Frozen Chest;Marrow;Unholy Trident;Frost Helmet;Frost Breastplate;Frost Leggings;Tin Helmet;Tin Chainmail;Tin Greaves;Lead Helmet;Lead Chainmail;Lead Greaves;Tungsten Helmet;Tungsten Chainmail;Tungsten Greaves;Platinum Helmet;Platinum Chainmail;Platinum Greaves;Tin Ore;Lead Ore;Tungsten Ore;Platinum Ore;Tin Bar;Lead Bar;Tungsten Bar;Platinum Bar;Tin Watch;Tungsten Watch;Platinum Watch;Tin Chandelier;Tungsten Chandelier;Platinum Chandelier;Platinum Candle;Platinum Candelabra;Platinum Crown;Lead Anvil;Tin Brick;Tungsten Brick;Platinum Brick;Tin Brick Wall;Tungsten Brick Wall;Platinum Brick Wall;Beam Sword;Ice Blade;Ice Bow;Frost Staff;Wood Helmet;Wood Breastplate;Wood Greaves;Ebonwood Helmet;Ebonwood Breastplate;Ebonwood Greaves;Rich Mahogany Helmet;Rich Mahogany Breastplate;Rich Mahogany Greaves;Pearlwood Helmet;Pearlwood Breastplate;Pearlwood Greaves;Amethyst Staff;Topaz Staff;Sapphire Staff;Emerald Staff;Ruby Staff;Diamond Staff;Grass Wall;Jungle Wall;Flower Wall;Jetpack;Butterfly Wings;Cactus Wall;Cloud;Cloud Wall;Seaweed;Rune Hat;Rune Robe;Mushroom Spear;Terra Blade;Grenade Launcher;Rocket Launcher;Proximity Mine Launcher;Fairy Wings;Slime Block;Flesh Block;Mushroom Wall;Rain Cloud;Bone Block;Frozen Slime Block;Bone Block Wall;Slime Block Wall;Flesh Block Wall;Rocket I;Rocket II;Rocket III;Rocket IV;Asphalt Block;Cobalt Pickaxe;Mythril Pickaxe;Adamantite Pickaxe;Clentaminator;Green Solution;Blue Solution;Purple Solution;Dark Blue Solution;Red Solution;Harpy Wings;Bone Wings;Hammush;Nettle Burst;Ankh Banner;Snake Banner;Omega Banner;Crimson Helmet;Crimson Scalemail;Crimson Greaves;Blood Butcherer;Tendon Bow;Flesh Grinder;Deathbringer Pickaxe;Blood Lust Cluster;The Undertaker;The Meatball;The Rotted Fork;Snow Hood;Snow Coat;Snow Pants;Living Wood Chair;Cactus Chair;Bone Chair;Flesh Chair;Mushroom Chair;Bone Work Bench;Cactus Work Bench;Flesh Work Bench;Mushroom Work Bench;Slime Work Bench;Cactus Door;Flesh Door;Mushroom Door;Living Wood Door;Bone Door;Flame Wings;Frozen Wings;Spectre Wings;Sunplate Block;Disc Wall;Skyware Chair;Bone Table;Flesh Table;Living Wood Table;Skyware Table;Living Wood Chest;Living Wood Wand;Purple Ice Block;Pink Ice Block;Red Ice Block;Crimstone Block;Skyware Door;Skyware Chest;Steampunk Hat;Steampunk Shirt;Steampunk Pants;Bee Hat;Bee Shirt;Bee Pants;World Banner;Sun Banner;Gravity Banner;Pharaohs Mask;Actuator;Blue Wrench;Green Wrench;Blue Pressure Plate;Yellow Pressure Plate;Discount Card;Lucky Coin;Unicorn on a Stick;Sandstorm in a Bottle;Boreal Wood Sofa;Beach Ball;Charm of Myths;Moon Shell;Star Veil;Water Walking Boots;Tiara;Princess Dress;Pharaohs Robe;Green Cap;Mushroom Cap;Tam O Shanter;Mummy Mask;Mummy Shirt;Mummy Pants;Cowboy Hat;Cowboy Jacket;Cowboy Pants;Pirate Hat;Pirate Shirt;Pirate Pants;Viking Helmet;Crimtane Ore;Cactus Sword;Cactus Pickaxe;Ice Brick;Ice Brick Wall;Adhesive Bandage;Armor Polish;Bezoar;Blindfold;Fast Clock;Megaphone;Nazar;Vitamins;Trifold Map;Cactus Helmet;Cactus Breastplate;Cactus Leggings;Power Glove;Lightning Boots;Sun Stone;Moon Stone;Armor Bracing;Medicated Bandage;The Plan;Countercurse Mantra;Coin Gun;Lava Charm;Obsidian Water Walking Boots;Lava Waders;Pure Water Fountain;Desert Water Fountain;Shadewood;Shadewood Door;Shadewood Platform;Shadewood Chest;Shadewood Chair;Shadewood Work Bench;Shadewood Table;Shadewood Dresser;Shadewood Piano;Shadewood Bed;Shadewood Sword;Shadewood Hammer;Shadewood Bow;Shadewood Helmet;Shadewood Breastplate;Shadewood Greaves;Shadewood Wall;Cannon;Cannonball;Flare Gun;Flare;Bone Wand;Leaf Wand;Flying Carpet;Avenger Emblem;Mechanical Glove;Land Mine;Paladins Shield;Web Slinger;Jungle Water Fountain;Icy Water Fountain;Corrupt Water Fountain;Crimson Water Fountain;Hallowed Water Fountain;Blood Water Fountain;Umbrella;Chlorophyte Ore;Steampunk Wings;Snowball;Ice Skates;Snowball Launcher;Web Covered Chest;Climbing Claws;Ancient Iron Helmet;Ancient Gold Helmet;Ancient Shadow Helmet;Ancient Shadow Scalemail;Ancient Shadow Greaves;Ancient Necro Helmet;Ancient Cobalt Helmet;Ancient Cobalt Breastplate;Ancient Cobalt Leggings;Black Belt;Boomstick;Rope;Campfire;Marshmallow;Marshmallow on a Stick;Cooked Marshmallow;Red Rocket;Green Rocket;Blue Rocket;Yellow Rocket;Ice Torch;Shoe Spikes;Tiger Climbing Gear;Tabi;Pink Snow Hood;Pink Snow Coat;Pink Snow Pants;Pink Thread;Mana Regeneration Band;Sandstorm in a Balloon;Master Ninja Gear;Rope Coil;Blowgun;Blizzard in a Bottle;Frostburn Arrow;Enchanted Sword;Pickaxe Axe;Cobalt Waraxe;Mythril Waraxe;Adamantite Waraxe;Eaters Bone;Blend-O-Matic;Meat Grinder;Extractinator;Solidifier;Amber;Confetti Gun;Chlorophyte Mask;Chlorophyte Helmet;Chlorophyte Headgear;Chlorophyte Plate Mail;Chlorophyte Greaves;Chlorophyte Bar;Red Dye;Orange Dye;Yellow Dye;Lime Dye;Green Dye;Teal Dye;Cyan Dye;Sky Blue Dye;Blue Dye;Purple Dye;Violet Dye;Pink Dye;Red and Black Dye;Orange and Black Dye;Yellow and Black Dye;Lime and Black Dye;Green and Black Dye;Teal and Black Dye;Cyan and Black Dye;Sky Blue and Black Dye;Blue and Black Dye;Purple and Black Dye;Violet and Black Dye;Pink and Black Dye;Flame Dye;Flame and Black Dye;Green Flame Dye;Green Flame and Black Dye;Blue Flame Dye;Blue Flame and Black Dye;Silver Dye;Bright Red Dye;Bright Orange Dye;Bright Yellow Dye;Bright Lime Dye;Bright Green Dye;Bright Teal Dye;Bright Cyan Dye;Bright Sky Blue Dye;Bright Blue Dye;Bright Purple Dye;Bright Violet Dye;Bright Pink Dye;Black Dye;Red and Silver Dye;Orange and Silver Dye;Yellow and Silver Dye;Lime and Silver Dye;Green and Silver Dye;Teal and Silver Dye;Cyan and Silver Dye;Sky Blue and Silver Dye;Blue and Silver Dye;Purple and Silver Dye;Violet and Silver Dye;Pink and Silver Dye;Intense Flame Dye;Intense Green Flame Dye;Intense Blue Flame Dye;Rainbow Dye;Intense Rainbow Dye;Yellow Gradient Dye;Cyan Gradient Dye;Violet Gradient Dye;Paintbrush;Paint Roller;Red Paint;Orange Paint;Yellow Paint;Lime Paint;Green Paint;Teal Paint;Cyan Paint;Sky Blue Paint;Blue Paint;Purple Paint;Violet Paint;Pink Paint;Deep Red Paint;Deep Orange Paint;Deep Yellow Paint;Deep Lime Paint;Deep Green Paint;Deep Teal Paint;Deep Cyan Paint;Deep Sky Blue Paint;Deep Blue Paint;Deep Purple Paint;Deep Violet Paint;Deep Pink Paint;Black Paint;White Paint;Gray Paint;Paint Scraper;Lihzahrd Brick;Lihzahrd Brick Wall;Slush Block;Palladium Ore;Orichalcum Ore;Titanium Ore;Teal Mushroom;Green Mushroom;Sky Blue Flower;Yellow Marigold;Blue Berries;Lime Kelp;Pink Prickly Pear;Orange Bloodroot;Red Husk;Cyan Husk;Violet Husk;Purple Mucus;Black Ink;Dye Vat;Bee Gun;Possessed Hatchet;Bee Keeper;Hive;Honey Block;Hive Wall;Crispy Honey Block;Honey Bucket;Hive Wand;Beenade;Gravity Globe;Honey Comb;Abeemination;Bottled Honey;Rain Hat;Rain Coat;Lihzahrd Door;Dungeon Door;Lead Door;Iron Door;Temple Key;Lihzahrd Chest;Lihzahrd Chair;Lihzahrd Table;Lihzahrd Work Bench;Super Dart Trap;Flame Trap;Spiky Ball Trap;Spear Trap;Wooden Spike;Lihzahrd Pressure Plate;Lihzahrd Statue;Lihzahrd Watcher Statue;Lihzahrd Guardian Statue;Wasp Gun;Piranha Gun;Pygmy Staff;Pygmy Necklace;Tiki Mask;Tiki Shirt;Tiki Pants;Leaf Wings;Blizzard in a Balloon;Bundle of Balloons;Bat Wings;Bone Sword;Hercules Beetle;Smoke Bomb;Bone Key;Nectar;Tiki Totem;Lizard Egg;Grave Marker;Cross Grave Marker;Headstone;Gravestone;Obelisk;Leaf Blower;Chlorophyte Bullet;Parrot Cracker;Strange Glowing Mushroom;Seedling;Wisp in a Bottle;Palladium Bar;Palladium Sword;Palladium Pike;Palladium Repeater;Palladium Pickaxe;Palladium Drill;Palladium Chainsaw;Orichalcum Bar;Orichalcum Sword;Orichalcum Halberd;Orichalcum Repeater;Orichalcum Pickaxe;Orichalcum Drill;Orichalcum Chainsaw;Titanium Bar;Titanium Sword;Titanium Trident;Titanium Repeater;Titanium Pickaxe;Titanium Drill;Titanium Chainsaw;Palladium Mask;Palladium Helmet;Palladium Headgear;Palladium Breastplate;Palladium Leggings;Orichalcum Mask;Orichalcum Helmet;Orichalcum Headgear;Orichalcum Breastplate;Orichalcum Leggings;Titanium Mask;Titanium Helmet;Titanium Headgear;Titanium Breastplate;Titanium Leggings;Orichalcum Anvil;Titanium Forge;Palladium Waraxe;Orichalcum Waraxe;Titanium Waraxe;Hallowed Bar;Chlorophyte Claymore;Chlorophyte Saber;Chlorophyte Partisan;Chlorophyte Shotbow;Chlorophyte Pickaxe;Chlorophyte Drill;Chlorophyte Chainsaw;Chlorophyte Greataxe;Chlorophyte Warhammer;Chlorophyte Arrow;Amethyst Hook;Topaz Hook;Sapphire Hook;Emerald Hook;Ruby Hook;Diamond Hook;Amber Mosquito;Umbrella Hat;Nimbus Rod;Orange Torch;Crimsand Block;Bee Cloak;Eye of the Golem;Honey Balloon;Blue Horseshoe Balloon;White Horseshoe Balloon;Yellow Horseshoe Balloon;Frozen Turtle Shell;Sniper Rifle;Venus Magnum;Crimson Rod;Crimtane Bar;Stynger;Flower Pow;Rainbow Gun;Stynger Bolt;Chlorophyte Jackhammer;Teleporter;Flower of Frost;Uzi;Magnet Sphere;Purple Stained Glass;Yellow Stained Glass;Blue Stained Glass;Green Stained Glass;Red Stained Glass;Multicolored Stained Glass;Skeletron Hand;Skull;Balla Hat;Gangsta Hat;Sailor Hat;Eye Patch;Sailor Shirt;Sailor Pants;Skeletron Mask;Amethyst Robe;Topaz Robe;Sapphire Robe;Emerald Robe;Ruby Robe;Diamond Robe;White Tuxedo Shirt;White Tuxedo Pants;Panic Necklace;Life Fruit;Lihzahrd Altar;Lihzahrd Power Cell;Picksaw;Heat Ray;Staff of Earth;Golem Fist;Water Chest;Binoculars;Rifle Scope;Destroyer Emblem;High Velocity Bullet;Jellyfish Necklace;Zombie Arm;The Axe;Ice Sickle;Clothier Voodoo Doll;Poison Staff;Slime Staff;Poison Dart;Eye Spring;Toy Sled;Book of Skulls;KO Cannon;Pirate Map;Turtle Helmet;Turtle Scale Mail;Turtle Leggings;Snowball Cannon;Bone Pickaxe;Magic Quiver;Magma Stone;Obsidian Rose;Bananarang;Chain Knife;Rod of Discord;Death Sickle;Turtle Shell;Tissue Sample;Vertebra;Bloody Spine;Ichor;Ichor Torch;Ichor Arrow;Ichor Bullet;Golden Shower;Bunny Cannon;Explosive Bunny;Vial of Venom;Flask of Venom;Venom Arrow;Venom Bullet;Fire Gauntlet;Cog;Confetti;Nanites;Explosive Powder;Gold Dust;Party Bullet;Nano Bullet;Exploding Bullet;Golden Bullet;Flask of Cursed Flames;Flask of Fire;Flask of Gold;Flask of Ichor;Flask of Nanites;Flask of Party;Flask of Poison;Eye of Cthulhu Trophy;Eater of Worlds Trophy;Brain of Cthulhu Trophy;Skeletron Trophy;Queen Bee Trophy;Wall of Flesh Trophy;Destroyer Trophy;Skeletron Prime Trophy;Retinazer Trophy;Spazmatism Trophy;Plantera Trophy;Golem Trophy;Blood Moon Rising;The Hanged Man;Glory of the Fire;Bone Warp;Wall Skeleton;Hanging Skeleton;Blue Slab Wall;Blue Tiled Wall;Pink Slab Wall;Pink Tiled Wall;Green Slab Wall;Green Tiled Wall;Blue Brick Platform;Pink Brick Platform;Green Brick Platform;Metal Shelf;Brass Shelf;Wood Shelf;Brass Lantern;Caged Lantern;Carriage Lantern;Alchemy Lantern;Diabolist Lamp;Oil Rag Sconce;Blue Dungeon Chair;Blue Dungeon Table;Blue Dungeon Work Bench;Green Dungeon Chair;Green Dungeon Table;Green Dungeon Work Bench;Pink Dungeon Chair;Pink Dungeon Table;Pink Dungeon Work Bench;Blue Dungeon Candle;Green Dungeon Candle;Pink Dungeon Candle;Blue Dungeon Vase;Green Dungeon Vase;Pink Dungeon Vase;Blue Dungeon Door;Green Dungeon Door;Pink Dungeon Door;Blue Dungeon Bookcase;Green Dungeon Bookcase;Pink Dungeon Bookcase;Catacomb;Dungeon Shelf;Skellington J Skellingsworth;The Cursed Man;The Eye Sees the End;Something Evil is Watching You;The Twins Have Awoken;The Screamer;Goblins Playing Poker;Dryadisque;Sunflowers;Terrarian Gothic;Beanie;Imbuing Station;Star in a Bottle;Empty Bullet;Impact;Powered by Birds;The Destroyer;The Persistency of Eyes;Unicorn Crossing the Hallows;Great Wave;Starry Night;Guide Picasso;The Guardians Gaze;Father of Someone;Nurse Lisa;Shadowbeam Staff;Inferno Fork;Spectre Staff;Wooden Fence;Lead Fence;Bubble Machine;Bubble Wand;Marching Bones Banner;Necromantic Sign;Rusted Company Standard;Ragged Brotherhood Sigil;Molten Legion Flag;Diabolic Sigil;Obsidian Platform;Obsidian Door;Obsidian Chair;Obsidian Table;Obsidian Work Bench;Obsidian Vase;Obsidian Bookcase;Hellbound Banner;Hell Hammer Banner;Helltower Banner;Lost Hopes of Man Banner;Obsidian Watcher Banner;Lava Erupts Banner;Blue Dungeon Bed;Green Dungeon Bed;Pink Dungeon Bed;Obsidian Bed;Waldo;Darkness;Dark Soul Reaper;Land;Trapped Ghost;Demons Eye;Finding Gold;First Encounter;Good Morning;Underground Reward;Through the Window;Place Above the Clouds;Do Not Step on the Grass;Cold Waters in the White Land;Lightless Chasms;The Land of Deceiving Looks;Daylight;Secret of the Sands;Deadland Comes Alive;Evil Presence;Sky Guardian;American Explosive;Discover;Hand Earth;Old Miner;Skelehead;Facing the Cerebral Mastermind;Lake of Fire;Trio Super Heroes;Spectre Hood;Spectre Robe;Spectre Pants;Spectre Pickaxe;Spectre Hamaxe;Ectoplasm;Gothic Chair;Gothic Table;Gothic Work Bench;Gothic Bookcase;Paladins Hammer;SWAT Helmet;Bee Wings;Giant Harpy Feather;Bone Feather;Fire Feather;Ice Feather;Broken Bat Wing;Tattered Bee Wing;Large Amethyst;Large Topaz;Large Sapphire;Large Emerald;Large Ruby;Large Diamond;Jungle Chest;Corruption Chest;Crimson Chest;Hallowed Chest;Ice Chest;Jungle Key;Corruption Key;Crimson Key;Hallowed Key;Frozen Key;Imp Face;Ominous Presence;Shining Moon;Living Gore;Flowing Magma;Spectre Paintbrush;Spectre Paint Roller;Spectre Paint Scraper;Shroomite Headgear;Shroomite Mask;Shroomite Helmet;Shroomite Breastplate;Shroomite Leggings;Autohammer;Shroomite Bar;S.D.M.G.;Cenxs Tiara;Cenxs Breastplate;Cenxs Leggings;Crownos Mask;Crownos Breastplate;Crownos Leggings;Wills Helmet;Wills Breastplate;Wills Leggings;Jims Helmet;Jims Breastplate;Jims Leggings;Aarons Helmet;Aarons Breastplate;Aarons Leggings;Vampire Knives;Broken Hero Sword;Scourge of the Corruptor;Staff of the Frost Hydra;The Creation of the Guide;The Merchant;Crowno Devours His Lunch;Rare Enchantment;Glorious Night;Sweetheart Necklace;Flurry Boots;D-Towns Helmet;D-Towns Breastplate;D-Towns Leggings;D-Towns Wings;Wills Wings;Crownos Wings;Cenxs Wings;Cenxs Dress;Cenxs Dress Pants;Palladium Column;Palladium Column Wall;Bubblegum Block;Bubblegum Block Wall;Titanstone Block;Titanstone Block Wall;Magic Cuffs;Music Box (Snow);Music Box (Space Night);Music Box (Crimson);Music Box (Boss 4);Music Box (Alt Overworld Day);Music Box (Rain);Music Box (Ice);Music Box (Desert);Music Box (Ocean Day);Music Box (Dungeon);Music Box (Plantera);Music Box (Boss 5);Music Box (Temple);Music Box (Eclipse);Music Box (Mushrooms);Butterfly Dust;Ankh Charm;Ankh Shield;Blue Flare;Angler Fish Banner;Angry Nimbus Banner;Anomura Fungus Banner;Antlion Banner;Arapaima Banner;Armored Skeleton Banner;Cave Bat Banner;Bird Banner;Black Recluse Banner;Blood Feeder Banner;Blood Jelly Banner;Blood Crawler Banner;Bone Serpent Banner;Bunny Banner;Chaos Elemental Banner;Mimic Banner;Clown Banner;Corrupt Bunny Banner;Corrupt Goldfish Banner;Crab Banner;Crimera Banner;Crimson Axe Banner;Cursed Hammer Banner;Demon Banner;Demon Eye Banner;Derpling Banner;Eater of Souls Banner;Enchanted Sword Banner;Frozen Zombie Banner;Face Monster Banner;Floaty Gross Banner;Flying Fish Banner;Flying Snake Banner;Frankenstein Banner;Fungi Bulb Banner;Fungo Fish Banner;Gastropod Banner;Goblin Thief Banner;Goblin Sorcerer Banner;Goblin Peon Banner;Goblin Scout Banner;Goblin Warrior Banner;Goldfish Banner;Harpy Banner;Hellbat Banner;Herpling Banner;Hornet Banner;Ice Elemental Banner;Icy Merman Banner;Fire Imp Banner;Blue Jellyfish Banner;Jungle Creeper Banner;Lihzahrd Banner;Man Eater Banner;Meteor Head Banner;Moth Banner;Mummy Banner;Mushi Ladybug Banner;Parrot Banner;Pigron Banner;Piranha Banner;Pirate Deckhand Banner;Pixie Banner;Raincoat Zombie Banner;Reaper Banner;Shark Banner;Skeleton Banner;Dark Caster Banner;Blue Slime Banner;Snow Flinx Banner;Wall Creeper Banner;Spore Zombie Banner;Swamp Thing Banner;Giant Tortoise Banner;Toxic Sludge Banner;Umbrella Slime Banner;Unicorn Banner;Vampire Banner;Vulture Banner;Nymph Banner;Werewolf Banner;Wolf Banner;World Feeder Banner;Worm Banner;Wraith Banner;Wyvern Banner;Zombie Banner;Glass Platform;Glass Chair;Golden Chair;Golden Toilet;Bar Stool;Honey Chair;Steampunk Chair;Glass Door;Golden Door;Honey Door;Steampunk Door;Glass Table;Banquet Table;Bar;Golden Table;Honey Table;Steampunk Table;Glass Bed;Golden Bed;Honey Bed;Steampunk Bed;Living Wood Wall;Fart in a Jar;Pumpkin;Pumpkin Wall;Hay;Hay Wall;Spooky Wood;Spooky Wood Wall;Pumpkin Helmet;Pumpkin Breastplate;Pumpkin Leggings;Candy Apple;Soul Cake;Nurse Hat;Nurse Shirt;Nurse Pants;Wizards Hat;Guy Fawkes Mask;Dye Trader Robe;Steampunk Goggles;Cyborg Helmet;Cyborg Shirt;Cyborg Pants;Creeper Mask;Creeper Shirt;Creeper Pants;Cat Mask;Cat Shirt;Cat Pants;Ghost Mask;Ghost Shirt;Pumpkin Mask;Pumpkin Shirt;Pumpkin Pants;Robot Mask;Robot Shirt;Robot Pants;Unicorn Mask;Unicorn Shirt;Unicorn Pants;Vampire Mask;Vampire Shirt;Vampire Pants;Witch Hat;Leprechaun Hat;Leprechaun Shirt;Leprechaun Pants;Pixie Shirt;Pixie Pants;Princess Hat;Princess Dress;Goodie Bag;Witch Dress;Witch Boots;Bride of Frankenstein Mask;Bride of Frankenstein Dress;Karate Tortoise Mask;Karate Tortoise Shirt;Karate Tortoise Pants;Candy Corn Rifle;Candy Corn;Jack O Lantern Launcher;Explosive Jack O Lantern;Sickle;Pumpkin Pie;Scarecrow Hat;Scarecrow Shirt;Scarecrow Pants;Cauldron;Pumpkin Chair;Pumpkin Door;Pumpkin Table;Pumpkin Work Bench;Pumpkin Platform;Tattered Fairy Wings;Spider Egg;Magical Pumpkin Seed;Bat Hook;Bat Scepter;Raven Staff;Jungle Key;Corruption Key;Crimson Key;Hallowed Key;Frozen Key;Hanging Jack O Lantern;Rotten Egg;Unlucky Yarn;Black Fairy Dust;Jackelier;Jack O Lantern;Spooky Chair;Spooky Door;Spooky Table;Spooky Work Bench;Spooky Wood Platform;Reaper Hood;Reaper Robe;Fox Mask;Fox Shirt;Fox Pants;Cat Ears;Bloody Machete;The Horsemans Blade;Bladed Glove;Pumpkin Seed;Spooky Hook;Spooky Wings;Spooky Twig;Spooky Helmet;Spooky Breastplate;Spooky Leggings;Stake Launcher;Stake;Cursed Sapling;Space Creature Mask;Space Creature Shirt;Space Creature Pants;Wolf Mask;Wolf Shirt;Wolf Pants;Pumpkin Moon Medallion;Necromantic Scroll;Jacking Skeletron;Bitter Harvest;Blood Moon Countess;Hallows Eve;Morbid Curiosity;Treasure Hunter Shirt;Treasure Hunter Pants;Dryad Coverings;Dryad Loincloth;Mourning Wood Trophy;Pumpking Trophy;Jack O Lantern Mask;Sniper Scope;Heart Lantern;Jellyfish Diving Gear;Arctic Diving Gear;Frostspark Boots;Fart in a Balloon;Papyrus Scarab;Celestial Stone;Hoverboard;Candy Cane;Sugar Plum;Present;Red Ryder;Festive Wings;Pine Tree Block;Christmas Tree;Star Topper 1;Star Topper 2;Star Topper 3;Bow Topper;White Garland;White and Red Garland;Red Garland;Red and Green Garland;Green Garland;Green and White Garland;Multicolored Bulb;Red Bulb;Yellow Bulb;Green Bulb;Red and Green Bulb;Yellow and Green Bulb;Red and Yellow Bulb;White Bulb;White and Red Bulb;White and Yellow Bulb;White and Green Bulb;Multicolored Lights;Red Lights;Green Lights;Blue Lights;Yellow Lights;Red and Yellow Lights;Red and Green Lights;Yellow and Green Lights;Blue and Green Lights;Red and Blue Lights;Blue and Yellow Lights;Giant Bow;Reindeer Antlers;Holly;Candy Cane Sword;Elf Melter;Christmas Pudding;Eggnog;Star Anise;Reindeer Bells;Candy Cane Hook;Christmas Hook;Candy Cane Pickaxe;Fruitcake Chakram;Sugar Cookie;Gingerbread Cookie;Hand Warmer;Coal;Toolbox;Pine Door;Pine Chair;Pine Table;Dog Whistle;Christmas Tree Sword;Chain Gun;Razorpine;Blizzard Staff;Mrs. Claus Hat;Mrs. Claus Shirt;Mrs. Claus Heels;Parka Hood;Parka Coat;Parka Pants;Snow Hat;Ugly Sweater;Tree Mask;Tree Shirt;Tree Trunks;Elf Hat;Elf Shirt;Elf Pants;Snowman Cannon;North Pole;Christmas Tree Wallpaper;Ornament Wallpaper;Candy Cane Wallpaper;Festive Wallpaper;Stars Wallpaper;Squiggles Wallpaper;Snowflake Wallpaper;Krampus Horn Wallpaper;Bluegreen Wallpaper;Grinch Finger Wallpaper;Naughty Present;Baby Grinchs Mischief Whistle;Ice Queen Trophy;Santa-NK1 Trophy;Everscream Trophy;Music Box (Pumpkin Moon);Music Box (Alt Underground);Music Box (Frost Moon);Brown Paint;Shadow Paint;Negative Paint;Team Dye;Amethyst Gemspark Block;Topaz Gemspark Block;Sapphire Gemspark Block;Emerald Gemspark Block;Ruby Gemspark Block;Diamond Gemspark Block;Amber Gemspark Block;Life Hair Dye;Mana Hair Dye;Depth Hair Dye;Money Hair Dye;Time Hair Dye;Team Hair Dye;Biome Hair Dye;Party Hair Dye;Rainbow Hair Dye;Speed Hair Dye;Angel Halo;Fez;Womannequin;Hair Dye Remover;Bug Net;Firefly;Firefly in a Bottle;Monarch Butterfly;Purple Emperor Butterfly;Red Admiral Butterfly;Ulysses Butterfly;Sulphur Butterfly;Tree Nymph Butterfly;Zebra Swallowtail Butterfly;Julia Butterfly;Worm;Mouse;Lightning Bug;Lightning Bug in a Bottle;Snail;Glowing Snail;Fancy Gray Wallpaper;Ice Floe Wallpaper;Music Wallpaper;Purple Rain Wallpaper;Rainbow Wallpaper;Sparkle Stone Wallpaper;Starlit Heaven Wallpaper;Bird;Blue Jay;Cardinal;Squirrel;Bunny;Cactus Bookcase;Ebonwood Bookcase;Flesh Bookcase;Honey Bookcase;Steampunk Bookcase;Glass Bookcase;Rich Mahogany Bookcase;Pearlwood Bookcase;Spooky Bookcase;Skyware Bookcase;Lihzahrd Bookcase;Frozen Bookcase;Cactus Lantern;Ebonwood Lantern;Flesh Lantern;Honey Lantern;Steampunk Lantern;Glass Lantern;Rich Mahogany Lantern;Pearlwood Lantern;Frozen Lantern;Lihzahrd Lantern;Skyware Lantern;Spooky Lantern;Frozen Door;Cactus Candle;Ebonwood Candle;Flesh Candle;Glass Candle;Frozen Candle;Rich Mahogany Candle;Pearlwood Candle;Lihzahrd Candle;Skyware Candle;Pumpkin Candle;Cactus Chandelier;Ebonwood Chandelier;Flesh Chandelier;Honey Chandelier;Frozen Chandelier;Rich Mahogany Chandelier;Pearlwood Chandelier;Lihzahrd Chandelier;Skyware Chandelier;Spooky Chandelier;Glass Chandelier;Cactus Bed;Flesh Bed;Frozen Bed;Lihzahrd Bed;Skyware Bed;Spooky Bed;Cactus Bathtub;Ebonwood Bathtub;Flesh Bathtub;Glass Bathtub;Frozen Bathtub;Rich Mahogany Bathtub;Pearlwood Bathtub;Lihzahrd Bathtub;Skyware Bathtub;Spooky Bathtub;Cactus Lamp;Ebonwood Lamp;Flesh Lamp;Glass Lamp;Frozen Lamp;Rich Mahogany Lamp;Pearlwood Lamp;Lihzahrd Lamp;Skyware Lamp;Spooky Lamp;Cactus Candelabra;Ebonwood Candelabra;Flesh Candelabra;Honey Candelabra;Steampunk Candelabra;Glass Candelabra;Rich Mahogany Candelabra;Pearlwood Candelabra;Frozen Candelabra;Lihzahrd Candelabra;Skyware Candelabra;Spooky Candelabra;Brain of Cthulhu Mask;Wall of Flesh Mask;Twin Mask;Skeletron Prime Mask;Queen Bee Mask;Plantera Mask;Golem Mask;Eater of Worlds Mask;Eye of Cthulhu Mask;Destroyer Mask;Blacksmith Rack;Carpentry Rack;Helmet Rack;Spear Rack;Sword Rack;Stone Slab;Sandstone Slab;Frog;Mallard Duck;Duck;Honey Bathtub;Steampunk Bathtub;Living Wood Bathtub;Shadewood Bathtub;Bone Bathtub;Honey Lamp;Steampunk Lamp;Living Wood Lamp;Shadewood Lamp;Golden Lamp;Bone Lamp;Living Wood Bookcase;Shadewood Bookcase;Golden Bookcase;Bone Bookcase;Living Wood Bed;Bone Bed;Living Wood Chandelier;Shadewood Chandelier;Golden Chandelier;Bone Chandelier;Living Wood Lantern;Shadewood Lantern;Golden Lantern;Bone Lantern;Living Wood Candelabra;Shadewood Candelabra;Golden Candelabra;Bone Candelabra;Living Wood Candle;Shadewood Candle;Golden Candle;Black Scorpion;Scorpion;Bubble Wallpaper;Copper Pipe Wallpaper;Ducky Wallpaper;Frost Core;Bunny Cage;Squirrel Cage;Mallard Duck Cage;Duck Cage;Bird Cage;Blue Jay Cage;Cardinal Cage;Waterfall Wall;Lavafall Wall;Crimson Seeds;Heavy Assembler;Copper Plating;Snail Cage;Glowing Snail Cage;Shroomite Digging Claw;Ammo Box;Monarch Butterfly Jar;Purple Emperor Butterfly Jar;Red Admiral Butterfly Jar;Ulysses Butterfly Jar;Sulphur Butterfly Jar;Tree Nymph Butterfly Jar;Zebra Swallowtail Butterfly Jar;Julia Butterfly Jar;Scorpion Cage;Black Scorpion Cage;Venom Staff;Spectre Mask;Frog Cage;Mouse Cage;Bone Welder;Flesh Cloning Vat;Glass Kiln;Lihzahrd Furnace;Living Loom;Sky Mill;Ice Machine;Beetle Helmet;Beetle Scale Mail;Beetle Shell;Beetle Leggings;Steampunk Boiler;Honey Dispenser;Penguin;Penguin Cage;Worm Cage;Terrarium;Super Mana Potion;Ebonwood Fence;Rich Mahogany Fence;Pearlwood Fence;Shadewood Fence;Brick Layer;Extendo Grip;Paint Sprayer;Portable Cement Mixer;Beetle Husk;Celestial Magnet;Celestial Emblem;Celestial Cuffs;Peddlers Hat;Pulse Bow;Large Dynasty Lantern;Dynasty Lamp;Dynasty Lantern;Large Dynasty Candle;Dynasty Chair;Dynasty Work Bench;Dynasty Chest;Dynasty Bed;Dynasty Bathtub;Dynasty Bookcase;Dynasty Cup;Dynasty Bowl;Dynasty Candle;Dynasty Clock;Golden Clock;Glass Clock;Honey Clock;Steampunk Clock;Fancy Dishes;Glass Bowl;Wine Glass;Living Wood Piano;Flesh Piano;Frozen Piano;Frozen Table;Honey Chest;Steampunk Chest;Honey Work Bench;Frozen Work Bench;Steampunk Work Bench;Glass Piano;Honey Piano;Steampunk Piano;Honey Cup;Chalice;Dynasty Table;Dynasty Wood;Red Dynasty Shingles;Blue Dynasty Shingles;White Dynasty Wall;Blue Dynasty Wall;Dynasty Door;Sake;Pad Thai;Pho;Revolver;Gatligator;Arcane Rune Wall;Water Gun;Katana;Ultrabright Torch;Magic Hat;Diamond Ring;Gi;Kimono;Mystic Robe;Beetle Wings;Tiger Skin;Leopard Skin;Zebra Skin;Crimson Cloak;Mysterious Cape;Red Cape;Winter Cape;Frozen Chair;Wood Fishing Pole;Bass;Reinforced Fishing Pole;Fiberglass Fishing Pole;Fisher of Souls;Golden Fishing Rod;Mechanics Rod;Sitting Ducks Fishing Pole;Trout;Salmon;Atlantic Cod;Tuna;Red Snapper;Neon Tetra;Armored Cavefish;Damselfish;Crimson Tigerfish;Frost Minnow;Princess Fish;Golden Carp;Specular Fish;Prismite;Variegated Lardfish;Flarefin Koi;Double Cod;Honeyfin;Obsidifish;Shrimp;Chaos Fish;Ebonkoi;Hemopiranha;Rockfish;Stinkfish;Mining Potion;Heartreach Potion;Calming Potion;Builder Potion;Titan Potion;Flipper Potion;Summoning Potion;Dangersense Potion;Purple Clubberfish;Obsidian Swordfish;Swordfish;Iron Fence;Wooden Crate;Iron Crate;Golden Crate;Old Shoe;Seaweed;Tin Can;Minecart Track;Reaver Shark;Sawtooth Shark;Minecart;Ammo Reservation Potion;Lifeforce Potion;Endurance Potion;Rage Potion;Inferno Potion;Wrath Potion;Recall Potion;Teleportation Potion;Love Potion;Stink Potion;Fishing Potion;Sonar Potion;Crate Potion;Shiverthorn Seeds;Shiverthorn;Warmth Potion;Fish Hook;Bee Headgear;Bee Breastplate;Bee Greaves;Hornet Staff;Imp Staff;Queen Spider Staff;Angler Hat;Angler Vest;Angler Pants;Spider Mask;Spider Breastplate;Spider Greaves;High Test Fishing Line;Angler Earring;Tackle Box;Blue Dungeon Piano;Green Dungeon Piano;Pink Dungeon Piano;Golden Piano;Obsidian Piano;Bone Piano;Cactus Piano;Spooky Piano;Skyware Piano;Lihzahrd Piano;Blue Dungeon Dresser;Green Dungeon Dresser;Pink Dungeon Dresser;Golden Dresser;Obsidian Dresser;Bone Dresser;Cactus Dresser;Spooky Dresser;Skyware Dresser;Honey Dresser;Lihzahrd Dresser;Sofa;Ebonwood Sofa;Rich Mahogany Sofa;Pearlwood Sofa;Shadewood Sofa;Blue Dungeon Sofa;Green Dungeon Sofa;Pink Dungeon Sofa;Golden Sofa;Obsidian Sofa;Bone Sofa;Cactus Sofa;Spooky Sofa;Skyware Sofa;Honey Sofa;Steampunk Sofa;Mushroom Sofa;Glass Sofa;Pumpkin Sofa;Lihzahrd Sofa;Seashell Hairpin;Mermaid Adornment;Mermaid Tail;Zephyr Fish;Fleshcatcher;Hotline Fishing Hook;Frog Leg;Anchor;Cooked Fish;Cooked Shrimp;Sashimi;Fuzzy Carrot;Scaly Truffle;Slimy Saddle;Bee Wax;Copper Plating Wall;Stone Slab Wall;Sail;Coralstone Block;Blue Jellyfish;Green Jellyfish;Pink Jellyfish;Blue Jellyfish Jar;Green Jellyfish Jar;Pink Jellyfish Jar;Life Preserver;Ships Wheel;Compass Rose;Wall Anchor;Goldfish Trophy;Bunnyfish Trophy;Swordfish Trophy;Sharkteeth Trophy;Batfish;Bumblebee Tuna;Catfish;Cloudfish;Cursedfish;Dirtfish;Dynamite Fish;Eater of Plankton;Fallen Starfish;The Fish of Cthulhu;Fishotron;Harpyfish;Hungerfish;Ichorfish;Jewelfish;Mirage Fish;Mutant Flinxfin;Pengfish;Pixiefish;Spiderfish;Tundra Trout;Unicorn Fish;Guide Voodoo Fish;Wyverntail;Zombie Fish;Amanita Fungifin;Angelfish;Bloody Manowar;Bonefish;Bunnyfish;Capn Tunabeard;Clownfish;Demonic Hellfish;Derpfish;Fishron;Infected Scabbardfish;Mudfish;Slimefish;Tropical Barracuda;King Slime Trophy;Ship in a Bottle;Hardy Saddle;Pressure Plate Track;King Slime Mask;Fin Wings;Treasure Map;Seaweed Planter;Pillagin Me Pixels;Fish Costume Mask;Fish Costume Shirt;Fish Costume Finskirt;Ginger Beard;Honeyed Goggles;Boreal Wood;Palm Wood;Boreal Wood Wall;Palm Wood Wall;Boreal Wood Fence;Palm Wood Fence;Boreal Wood Helmet;Boreal Wood Breastplate;Boreal Wood Greaves;Palm Wood Helmet;Palm Wood Breastplate;Palm Wood Greaves;Palm Wood Bow;Palm Wood Hammer;Palm Wood Sword;Palm Wood Platform;Palm Wood Bathtub;Palm Wood Bed;Palm Wood Bench;Palm Wood Candelabra;Palm Wood Candle;Palm Wood Chair;Palm Wood Chandelier;Palm Wood Chest;Palm Wood Sofa;Palm Wood Door;Palm Wood Dresser;Palm Wood Lantern;Palm Wood Piano;Palm Wood Table;Palm Wood Lamp;Palm Wood Work Bench;Optic Staff;Palm Wood Bookcase;Mushroom Bathtub;Mushroom Bed;Mushroom Bench;Mushroom Bookcase;Mushroom Candelabra;Mushroom Candle;Mushroom Chandelier;Mushroom Chest;Mushroom Dresser;Mushroom Lantern;Mushroom Lamp;Mushroom Piano;Mushroom Platform;Mushroom Table;Spider Staff;Boreal Wood Bathtub;Boreal Wood Bed;Boreal Wood Bookcase;Boreal Wood Candelabra;Boreal Wood Candle;Boreal Wood Chair;Boreal Wood Chandelier;Boreal Wood Chest;Boreal Wood Clock;Boreal Wood Door;Boreal Wood Dresser;Boreal Wood Lamp;Boreal Wood Lantern;Boreal Wood Piano;Boreal Wood Platform;Slime Bathtub;Slime Bed;Slime Bookcase;Slime Candelabra;Slime Candle;Slime Chair;Slime Chandelier;Slime Chest;Slime Clock;Slime Door;Slime Dresser;Slime Lamp;Slime Lantern;Slime Piano;Slime Platform;Slime Sofa;Slime Table;Pirate Staff;Slime Hook;Sticky Grenade;Beguiling Lyre;Duke Fishron Mask;Duke Fishron Trophy;Molotov Cocktail;Bone Clock;Cactus Clock;Ebonwood Clock;Frozen Clock;Lihzahrd Clock;Living Wood Clock;Rich Mahogany Clock;Flesh Clock;Mushroom Clock;Obsidian Clock;Palm Wood Clock;Pearlwood Clock;Pumpkin Clock;Shadewood Clock;Spooky Clock;Skyware Clock;Spider Fang;Falcon Blade;Fishron Wings;Slime Gun;Flairoon;Green Dungeon Chest;Pink Dungeon Chest;Blue Dungeon Chest;Bone Chest;Cactus Chest;Flesh Chest;Obsidian Chest;Pumpkin Chest;Spooky Chest;Tempest Staff;Razorblade Typhoon;Bubble Gun;Tsunami;Seashell;Starfish;Steampunk Platform;Skyware Platform;Living Wood Platform;Honey Platform;Skyware Work Bench;Glass Work Bench;Living Wood Work Bench;Flesh Sofa;Frozen Sofa;Living Wood Sofa;Pumpkin Dresser;Steampunk Dresser;Glass Dresser;Flesh Dresser;Pumpkin Lantern;Obsidian Lantern;Pumpkin Lamp;Obsidian Lamp;Blue Dungeon Lamp;Green Dungeon Lamp;Pink Dungeon Lamp;Honey Candle;Steampunk Candle;Spooky Candle;Obsidian Candle;Blue Dungeon Chandelier;Green Dungeon Chandelier;Pink Dungeon Chandelier;Steampunk Chandelier;Pumpkin Chandelier;Obsidian Chandelier;Blue Dungeon Bathtub;Green Dungeon Bathtub;Pink Dungeon Bathtub;Pumpkin Bathtub;Obsidian Bathtub;Golden Bathtub;Blue Dungeon Candelabra;Green Dungeon Candelabra;Pink Dungeon Candelabra;Obsidian Candelabra;Pumpkin Candelabra;Pumpkin Bed;Pumpkin Bookcase;Pumpkin Piano;Shark Statue;Truffle Worm;Apprentice Bait;Journeyman Bait;Master Bait;Amber Gemspark Wall;Offline Amber Gemspark Wall;Amethyst Gemspark Wall;Offline Amethyst Gemspark Wall;Diamond Gemspark Wall;Offline Diamond Gemspark Wall;Emerald Gemspark Wall;Offline Emerald Gemspark Wall;Ruby Gemspark Wall;Offline Ruby Gemspark Wall;Sapphire Gemspark Wall;Offline Sapphire Gemspark Wall;Topaz Gemspark Wall;Offline Topaz Gemspark Wall;Tin Plating Wall;Tin Plating;Waterfall Block;Lavafall Block;Confetti Block;Confetti Wall;Midnight Confetti Block;Midnight Confetti Wall;Weapon Rack;Fireworks Box;Living Fire Block;0 Statue;1 Statue;2 Statue;3 Statue;4 Statue;5 Statue;6 Statue;7 Statue;8 Statue;9 Statue;A Statue;B Statue;C Statue;D Statue;E Statue;F Statue;G Statue;H Statue;I Statue;J Statue;K Statue;L Statue;M Statue;N Statue;O Statue;P Statue;Q Statue;R Statue;S Statue;T Statue;U Statue;V Statue;W Statue;X Statue;Y Statue;Z Statue;Firework Fountain;Booster Track;Grasshopper;Grasshopper Cage;Music Box (Underground Crimson);Cactus Table;Cactus Platform;Boreal Wood Sword;Boreal Wood Hammer;Boreal Wood Bow;Glass Chest;Xeno Staff;Meteor Staff;Living Cursed Fire Block;Living Demon Fire Block;Living Frost Fire Block;Living Ichor Block;Living Ultrabright Fire Block;Gender Change Potion;Vortex Helmet;Vortex Breastplate;Vortex Leggings;Nebula Helmet;Nebula Breastplate;Nebula Leggings;Solar Flare Helmet;Solar Flare Breastplate;Solar Flare Leggings;Solar Tablet Fragment;Solar Tablet;Drill Containment Unit;Cosmic Car Key;Mothron Wings;Brain Scrambler;;;Vortex Drill;;Vortex Pickaxe;;;Nebula Drill;;Nebula Pickaxe;;;Solar Flare Drill;;Solar Flare Pickaxe;Honeyfall Block;Honeyfall Wall;Chlorophyte Brick Wall;Crimtane Brick Wall;Shroomite Plating Wall;Chlorophyte Brick;Crimtane Brick;Shroomite Plating;Laser Machinegun;Electrosphere Launcher;Xenopopper;Laser Drill;Mechanical Ruler;Anti-Gravity Hook;Moon Mask;Sun Mask;Martian Costume Mask;Martian Costume Shirt;Martian Costume Pants;Martian Uniform Helmet;Martian Uniform Torso;Martian Uniform Pants;Martian Astro Clock;Martian Bathtub;Martian Bed;Martian Hover Chair;Martian Chandelier;Martian Chest;Martian Door;Martian Dresser;Martian Holobookcase;Martian Hover Candle;Martian Lamppost;Martian Lantern;Martian Piano;Martian Platform;Martian Sofa;Martian Table;Martian Table Lamp;Martian Work Bench;Wooden Sink;Ebonwood Sink;Rich Mahogany Sink;Pearlwood Sink;Bone Sink;Flesh Sink;Living Wood Sink;Skyware Sink;Shadewood Sink;Lihzahrd Sink;Blue Dungeon Sink;Green Dungeon Sink;Pink Dungeon Sink;Obsidian Sink;Metal Sink;Glass Sink;Golden Sink;Honey Sink;Steampunk Sink;Pumpkin Sink;Spooky Sink;Frozen Sink;Dynasty Sink;Palm Wood Sink;Mushroom Sink;Boreal Wood Sink;Slime Sink;Cactus Sink;Martian Sink;Solar Cultist Hood;Lunar Cultist Hood;Solar Cultist Robe;Lunar Cultist Robe;Martian Conduit Plating;Martian Conduit Wall;HiTek Sunglasses;Martian Hair Dye;Martian Dye;Castle Marsberg;Martia Lisa;The Truth Is Up There;Smoke Block;Living Flame Dye;Living Rainbow Dye;Shadow Dye;Negative Dye;Living Ocean Dye;Brown Dye;Brown and Black Dye;Bright Brown Dye;Brown and Silver Dye;Wisp Dye;Pixie Dye;Influx Waver;;Charged Blaster Cannon;Chlorophyte Dye;Unicorn Wisp Dye;Infernal Wisp Dye;Vicious Powder;Vicious Mushroom;The Bees Knees;Gold Bird;Gold Bunny;Gold Butterfly;Gold Frog;Gold Grasshopper;Gold Mouse;Gold Worm;Sticky Dynamite;Angry Trapper Banner;Armored Viking Banner;Black Slime Banner;Blue Armored Bones Banner;Blue Cultist Archer Banner;Lunatic Devotee Banner;Blue Cultist Fighter Banner;Bone Lee Banner;Clinger Banner;Cochineal Beetle Banner;Corrupt Penguin Banner;Corrupt Slime Banner;Corruptor Banner;Crimslime Banner;Cursed Skull Banner;Cyan Beetle Banner;Devourer Banner;Diabolist Banner;Doctor Bones Banner;Dungeon Slime Banner;Dungeon Spirit Banner;Elf Archer Banner;Elf Copter Banner;Eyezor Banner;Flocko Banner;Ghost Banner;Giant Bat Banner;Giant Cursed Skull Banner;Giant Flying Fox Banner;Gingerbread Man Banner;Goblin Archer Banner;Green Slime Banner;Headless Horseman Banner;Hell Armored Bones Banner;Hellhound Banner;Hoppin Jack Banner;Ice Bat Banner;Ice Golem Banner;Ice Slime Banner;Ichor Sticker Banner;Illuminant Bat Banner;Illuminant Slime Banner;Jungle Bat Banner;Jungle Slime Banner;Krampus Banner;Lac Beetle Banner;Lava Bat Banner;Lava Slime Banner;Martian Brain Scrambler Banner;Martian Drone Banner;Martian Engineer Banner;Martian Gigazapper Banner;Martian Gray Grunt Banner;Martian Officer Banner;Martian Ray Gunner Banner;Martian Scutlix Gunner Banner;Martian Tesla Turret Banner;Mister Stabby Banner;Mother Slime Banner;Necromancer Banner;Nutcracker Banner;Paladin Banner;Penguin Banner;Pinky Banner;Poltergeist Banner;Possessed Armor Banner;Present Mimic Banner;Purple Slime Banner;Ragged Caster Banner;Rainbow Slime Banner;Raven Banner;Red Slime Banner;Rune Wizard Banner;Rusty Armored Bones Banner;Scarecrow Banner;Scutlix Banner;Skeleton Archer Banner;Skeleton Commando Banner;Skeleton Sniper Banner;Slimer Banner;Snatcher Banner;Snow Balla Banner;Snowman Gangsta Banner;Spiked Ice Slime Banner;Spiked Jungle Slime Banner;Splinterling Banner;Squid Banner;Tactical Skeleton Banner;The Groom Banner;Tim Banner;Undead Miner Banner;Undead Viking Banner;White Cultist Archer Banner;White Cultist Caster Banner;White Cultist Fighter Banner;Yellow Slime Banner;Yeti Banner;Zombie Elf Banner;Sparky;Vine Rope;Wormhole Potion;Summoner Emblem;Bewitching Table;Alchemy Table;Strange Brew;Spelunker Glowstick;Bone Arrow;Bone Torch;Vine Rope Coil;Life Drain;Dart Pistol;Dart Rifle;Crystal Dart;Cursed Dart;Ichor Dart;Chain Guillotines;Fetid Baghnakhs;Clinger Staff;Putrid Scent;Flesh Knuckles;Flower Boots;Seedler;Hellwing Bow;Tendon Hook;Thorn Hook;Illuminant Hook;Worm Hook;Skiphs Blood;Purple Ooze Dye;Reflective Silver Dye;Reflective Gold Dye;Blue Acid Dye;Daedalus Stormbow;Flying Knife;Bottomless Water Bucket;Super Absorbant Sponge;Gold Ring;Coin Ring;Greedy Ring;Fish Finder;Weather Radio;Hades Dye;Twilight Dye;Acid Dye;Glowing Mushroom Dye;Phase Dye;Magic Lantern;Music Box (Lunar Boss);Rainbow Torch;Cursed Campfire;Demon Campfire;Frozen Campfire;Ichor Campfire;Rainbow Campfire;Crystal Vile Shard;Shadowflame Bow;Shadowflame Hex Doll;Shadowflame Knife;Acorns;Cold Snap;Cursed Saint;Snowfellas;The Season;Bone Rattle;Architect Gizmo Pack;Crimson Heart;Meowmere;Enchanted Sundial;Star Wrath;Smooth Marble Block;Hellstone Brick Wall;Guide to Plant Fiber Cordage;Wand of Sparking;Gold Bird Cage;Gold Bunny Cage;Gold Butterfly Jar;Gold Frog Cage;Gold Grasshopper Cage;Gold Mouse Cage;Gold Worm Cage;Silk Rope;Web Rope;Silk Rope Coil;Web Rope Coil;Marble Block;Marble Wall;Smooth Marble Wall;Radar;Golden Lock Box;Granite Block;Smooth Granite Block;Granite Wall;Smooth Granite Wall;Royal Gel;Key of Night;Key of Light;Herb Bag;Javelin;Tally Counter;Sextant;Shield of Cthulhu;Butchers Chainsaw;Stopwatch;Meteorite Brick;Meteorite Brick Wall;Metal Detector;Endless Quiver;Endless Musket Pouch;Toxic Flask;Psycho Knife;Nail Gun;Nail;Night Vision Helmet;Celestial Shell;Pink Gel;Bouncy Glowstick;Pink Slime Block;Pink Torch;Bouncy Bomb;Bouncy Grenade;Peace Candle;Lifeform Analyzer;DPS Meter;Fishermans Pocket Guide;Goblin Tech;R.E.K. 3000;PDA;Cell Phone;Granite Chest;Meteorite Clock;Marble Clock;Granite Clock;Meteorite Door;Marble Door;Granite Door;Meteorite Dresser;Marble Dresser;Granite Dresser;Meteorite Lamp;Marble Lamp;Granite Lamp;Meteorite Lantern;Marble Lantern;Granite Lantern;Meteorite Piano;Marble Piano;Granite Piano;Meteorite Platform;Marble Platform;Granite Platform;Meteorite Sink;Marble Sink;Granite Sink;Meteorite Sofa;Marble Sofa;Granite Sofa;Meteorite Table;Marble Table;Granite Table;Meteorite Work Bench;Marble Work Bench;Granite Work Bench;Meteorite Bathtub;Marble Bathtub;Granite Bathtub;Meteorite Bed;Marble Bed;Granite Bed;Meteorite Bookcase;Marble Bookcase;Granite Bookcase;Meteorite Candelabra;Marble Candelabra;Granite Candelabra;Meteorite Candle;Marble Candle;Granite Candle;Meteorite Chair;Marble Chair;Granite Chair;Meteorite Chandelier;Marble Chandelier;Granite Chandelier;Meteorite Chest;Marble Chest;Magic Water Dropper;Golden Bug Net;Magic Lava Dropper;Magic Honey Dropper;Empty Dropper;Gladiator Helmet;Gladiator Breastplate;Gladiator Leggings;Reflective Dye;Enchanted Nightcrawler;Grubby;Sluggy;Buggy;Grub Soup;Bomb Fish;Frost Daggerfish;Sharpening Station;Ice Mirror;Sailfish Boots;Tsunami in a Bottle;Target Dummy;Corrupt Crate;Crimson Crate;Dungeon Crate;Sky Crate;Hallowed Crate;Jungle Crate;Crystal Serpent;Toxikarp;Bladetongue;Shark Tooth Necklace;Money Trough;Bubble;Daybloom Planter Box;Moonglow Planter Box;Deathweed Planter Box;Deathweed Planter Box;Blinkroot Planter Box;Waterleaf Planter Box;Shiverthorn Planter Box;Fireblossom Planter Box;Brain of Confusion;Worm Scarf;Balloon Pufferfish;Lazures Valkyrie Circlet;Lazures Valkyrie Cloak;Lazures Barrier Platform;Golden Cross Grave Marker;Golden Tombstone;Golden Grave Marker;Golden Gravestone;Golden Headstone;Crystal Block;Music Box (Martian Madness);Music Box (Pirate Invasion);Music Box (Hell);Crystal Block Wall;Trap Door;Tall Gate;Sharkron Balloon;Tax Collectors Hat;Tax Collectors Suit;Tax Collectors Pants;Bone Glove;Clothiers Jacket;Clothiers Pants;Dye Traders Turban;Deadly Sphere Staff;Green Horseshoe Balloon;Amber Horseshoe Balloon;Pink Horseshoe Balloon;Lava Lamp;Enchanted Nightcrawler Cage;Buggy Cage;Grubby Cage;Sluggy Cage;Slap Hand;Twilight Hair Dye;Blessed Apple;Spectre Bar;Code 1;Buccaneer Bandana;Buccaneer Tunic;Buccaneer Pantaloons;Obsidian Outlaw Hat;Obsidian Longcoat;Obsidian Pants;Medusa Head;Item Frame;Sandstone Block;Hardened Sand Block;Sandstone Wall;Hardened Ebonsand Block;Hardened Crimsand Block;Ebonsandstone Block;Crimsandstone Block;Wooden Yoyo;Malaise;Artery;Amazon;Cascade;Chik;Code 2;Rally;Yelets;Reds Throw;Valkyrie Yoyo;Amarok;Hel-Fire;Kraken;The Eye of Cthulhu;Red String;Orange String;Yellow String;Lime String;Green String;Teal String;Cyan String;Sky Blue String;Blue String;Purple String;Violet String;Pink String;Brown String;White String;Rainbow String;Black String;Black Counterweight;Blue Counterweight;Green Counterweight;Purple Counterweight;Red Counterweight;Yellow Counterweight;Format:C;Gradient;Valor;Treasure Bag (King Slime) (King Slime);Treasure Bag (Eye of Cthulhu) (Eye of Cthulhu);Treasure Bag (Eater of Worlds) (Eater of Worlds Head);Treasure Bag (Brain of Cthulhu) (Brain of Cthulhu);Treasure Bag (Queen Bee) (Queen Bee);Treasure Bag (Skeletron) (Skeletron Head);Treasure Bag (Wall of Flesh) (Wall of Flesh);Treasure Bag (The Destroyer) (The Destroyer Head);Treasure Bag (The Twins) (The Twins);Treasure Bag (Skeletron Prime) (Skeletron Prime);Treasure Bag (Plantera) (Plantera);Treasure Bag (Golem) (Golem);Treasure Bag (Duke Fishron) (Duke Fishron);Treasure Bag (Lunatic Cultist) (Lunatic Cultist);Treasure Bag (Moon Lord) (Moon Lord);Hive Pack;Yoyo Glove;Demon Heart;Spore Sac;Shiny Stone;Hardened Pearlsand Block;Pearlsandstone Block;Hardened Sand Wall;Hardened Ebonsand Wall;Hardened Crimsand Wall;Hardened Pearlsand Wall;Ebonsandstone Wall;Crimsandstone Wall;Pearlsandstone Wall;Desert Fossil;Desert Fossil Wall;Exotic Scimitar;Paintball Gun;Classy Cane;Stylish Scissors;Mechanical Cart;Mechanical Wheel Piece;Mechanical Wagon Piece;Mechanical Battery Piece;Lunatic Cultist Trophy;Martian Saucer Trophy;Flying Dutchman Trophy;Living Mahogany Wand;Rich Mahogany Leaf Wand;Fallen Tuxedo Shirt;Fallen Tuxedo Pants;Fireplace;Chimney;Yoyo Bag;Shrimpy Truffle;Arkhalis;Confetti Cannon;Music Box (The Towers);Music Box (Goblin Invasion);Lunatic Cultist Mask;Moon Lord Mask;Fossil Helmet;Fossil Plate;Fossil Greaves;Amber Staff;Bone Javelin;Bone Throwing Knife;Sturdy Fossil;Stardust Helmet;Stardust Plate;Stardust Leggings;Portal Gun;Strange Plant;Strange Plant;Strange Plant;Strange Plant;Terrarian;Goblin Warlock Banner;Salamander Banner;Giant Shelly Banner;Crawdad Banner;Fritz Banner;Creature From The Deep Banner;Dr. Man Fly Banner;Mothron Banner;Severed Hand Banner;The Possessed Banner;Butcher Banner;Psycho Banner;Deadly Sphere Banner;Nailhead Banner;Poisonous Spore Banner;Medusa Banner;Hoplite Banner;Granite Elemental Banner;Granite Golem Banner;Blood Zombie Banner;Drippler Banner;Tomb Crawler Banner;Dune Splicer Banner;Antlion Swarmer Banner;Antlion Charger Banner;Ghoul Banner;Lamia Banner;Desert Spirit Banner;Basilisk Banner;Sand Poacher Banner;Stargazer Banner;Milkyway Weaver Banner;Flow Invader Banner;Twinkle Popper Banner;Mini Star Cell Banner;Star Cell Banner;Corite Banner;Sroller Banner;Crawltipede Banner;Drakomire Rider Banner;Drakomire Banner;Selenian Banner;Predictor Banner;Brain Suckler Banner;Nebula Floater Banner;Evolution Beast Banner;Alien Larva Banner;Alien Queen Banner;Alien Hornet Banner;Vortexian Banner;Storm Diver Banner;Pirate Captain Banner;Pirate Deadeye Banner;Pirate Corsair Banner;Pirate Crossbower Banner;Martian Walker Banner;Red Devil Banner;Pink Jellyfish Banner;Green Jellyfish Banner;Dark Mummy Banner;Light Mummy Banner;Angry Bones Banner;Ice Tortoise Banner;Damage Booster;Life Booster;Mana Booster;Vortex Fragment;Nebula Fragment;Solar Fragment;Stardust Fragment;Luminite;Luminite Brick;Stardust Axe;Stardust Saw;Stardust Drill;Stardust Hammer;Stardust Pickaxe;Luminite Bar;Solar Wings;Vortex Booster;Nebula Mantle;Stardust Wings;Luminite Brick Wall;Solar Eruption;Stardust Cell Staff;Vortex Beater;Nebula Arcanum;Blood Water;Wedding Veil;Wedding Dress;Platinum Bow;Platinum Hammer;Platinum Axe;Platinum Shortsword;Platinum Broadsword;Platinum Pickaxe;Tungsten Bow;Tungsten Hammer;Tungsten Axe;Tungsten Shortsword;Tungsten Broadsword;Tungsten Pickaxe;Lead Bow;Lead Hammer;Lead Axe;Lead Shortsword;Lead Broadsword;Lead Pickaxe;Tin Bow;Tin Hammer;Tin Axe;Tin Shortsword;Tin Broadsword;Tin Pickaxe;Copper Bow;Copper Hammer;Copper Axe;Copper Shortsword;Copper Broadsword;Copper Pickaxe;Silver Bow;Silver Hammer;Silver Axe;Silver Shortsword;Silver Broadsword;Silver Pickaxe;Gold Bow;Gold Hammer;Gold Axe;Gold Shortsword;Gold Broadsword;Gold Pickaxe;Solar Flare Hamaxe;Vortex Hamaxe;Nebula Hamaxe;Stardust Hamaxe;Solar Dye;Nebula Dye;Vortex Dye;Stardust Dye;Void Dye;Stardust Dragon Staff;Bacon;Shifting Sands Dye;Mirage Dye;Shifting Pearlsands Dye;Vortex Monolith;Nebula Monolith;Stardust Monolith;Solar Monolith;Phantasm;Last Prism;Nebula Blaze;Daybreak;Super Healing Potion;Detonator;Celebration;Bouncy Dynamite;Happy Grenade;Ancient Manipulator;Flame and Silver Dye;Green Flame and Silver Dye;Blue Flame and Silver Dye;Reflective Copper Dye;Reflective Obsidian Dye;Reflective Metal Dye;Midnight Rainbow Dye;Black and White Dye;Bright Silver Dye;Silver and Black Dye;Red Acid Dye;Gel Dye;Pink Gel Dye;Red Squirrel;Gold Squirrel;Red Squirrel Cage;Gold Squirrel Cage;Luminite Bullet;Luminite Arrow;Lunar Portal Staff;Lunar Flare;Rainbow Crystal Staff;Lunar Hook;Solar Fragment Block;Vortex Fragment Block;Nebula Fragment Block;Stardust Fragment Block;Suspicious Looking Tentacle;Yoraiz0rs Uniform;Yoraiz0rs Skirt;Yoraiz0rs Spell;Yoraiz0rs Scowl;Jims Wings;Yoraiz0rs Recolored Goggles;Living Leaf Wall;Skiphs Mask;Skiphs Skin;Skiphs Bear Butt;Skiphs Paws;Lokis Helmet;Lokis Breastplate;Lokis Greaves;Lokis Wings;Sand Slime Banner;Sea Snail Banner;Moon Lord Trophy;Not a Kid, nor a Squid;Burning Hades Dye;Grim Dye;Lokis Dye;Shadowflame Hades Dye;Celestial Sigil;Logic Gate Lamp (Off);Logic Gate (AND);Logic Gate (OR);Logic Gate (NAND);Logic Gate (NOR);Logic Gate (XOR);Logic Gate (XNOR);Conveyor Belt (Clockwise);Conveyor Belt (Counter Clockwise);The Grand Design;Yellow Wrench;Logic Sensor (Day);Logic Sensor (Night);Logic Sensor (Player Above);Junction Box;Announcement Box;Logic Gate Lamp (On);Mechanical Lens;Actuation Rod;Red Team Block;Red Team Platform;Static Hook;Presserator;Multicolor Wrench;Pink Weighted Pressure Plate;Engineering Helmet;Companion Cube;Wire Bulb;Orange Weighted Pressure Plate;Purple Weighted Pressure Plate;Cyan Weighted Pressure Plate;Green Team Block;Blue Team Block;Yellow Team Block;Pink Team Block;White Team Block;Green Team Platform;Blue Team Platform;Yellow Team Platform;Pink Team Platform;White Team Platform;Large Amber;Ruby Gem Lock;Sapphire Gem Lock;Emerald Gem Lock;Topaz Gem Lock;Amethyst Gem Lock;Diamond Gem Lock;Amber Gem Lock;Squirrel Statue;Butterfly Statue;Worm Statue;Firefly Statue;Scorpion Statue;Snail Statue;Grasshopper Statue;Mouse Statue;Duck Statue;Penguin Statue;Frog Statue;Buggy Statue;Logic Gate Lamp (Faulty);Portal Gun Station;Trapped Chest;Trapped Gold Chest;Trapped Shadow Chest;Trapped Ebonwood Chest;Trapped Rich Mahogany Chest;Trapped Pearlwood Chest;Trapped Ivy Chest;Trapped Frozen Chest;Trapped Living Wood Chest;Trapped Skyware Chest;Trapped Shadewood Chest;Trapped Web Covered Chest;Trapped Lihzahrd Chest;Trapped Water Chest;Trapped Jungle Chest;Trapped Corruption Chest;Trapped Crimson Chest;Trapped Hallowed Chest;Trapped Ice Chest;Trapped Dynasty Chest;Trapped Honey Chest;Trapped Steampunk Chest;Trapped Palm Wood Chest;Trapped Mushroom Chest;Trapped Boreal Wood Chest;Trapped Slime Chest;Trapped Green Dungeon Chest;Trapped Pink Dungeon Chest;Trapped Blue Dungeon Chest;Trapped Bone Chest;Trapped Cactus Chest;Trapped Flesh Chest;Trapped Obsidian Chest;Trapped Pumpkin Chest;Trapped Spooky Chest;Trapped Glass Chest;Trapped Martian Chest;Trapped Meteorite Chest;Trapped Granite Chest;Trapped Marble Chest;ItemName.Fake_newchest1;ItemName.Fake_newchest2;Teal Pressure Pad;Wall Creeper Statue;Unicorn Statue;Drippler Statue;Wraith Statue;Bone Skeleton Statue;Undead Viking Statue;Medusa Statue;Harpy Statue;Pigron Statue;Hoplite Statue;Granite Golem Statue;Armed Zombie Statue;Blood Zombie Statue;Angler Tackle Bag;Geyser;Ultrabright Campfire;Bone Campfire;Pixel Box;Liquid Sensor (Water);Liquid Sensor (Lava);Liquid Sensor (Honey);Liquid Sensor (Any);Bundled Party Balloons;Balloon Animal;Party Hat;Silly Sunflower Petals;Silly Sunflower Tops;Silly Sunflower Bottoms;Silly Pink Balloon;Silly Purple Balloon;Silly Green Balloon;Blue Streamer;Green Streamer;Pink Streamer;Silly Balloon Machine;Silly Tied Balloon (Pink);Silly Tied Balloon (Purple);Silly Tied Balloon (Green);Pigronata;Party Center;Silly Tied Bundle of Balloons;Party Present;Slice of Cake;Cog Wall;Sandfall Wall;Snowfall Wall;Sandfall Block;Snowfall Block;Snow Cloud;Pedguins Hood;Pedguins Jacket;Pedguins Trousers;Silly Pink Balloon Wall;Silly Purple Balloon Wall;Silly Green Balloon Wall;0x33s Aviators;Blue Phasesaber;Red Phasesaber;Green Phasesaber;Purple Phasesaber;White Phasesaber;Yellow Phasesaber;Djinns Curse;Ancient Horn;Mandible Blade;Ancient Headdress;Ancient Garments;Ancient Slacks;Forbidden Mask;Forbidden Robes;Forbidden Treads;Spirit Flame;Sand Elemental Banner;Pocket Mirror;Magic Sand Dropper;Forbidden Fragment;Lamia Tail;Lamia Wraps;Lamia Mask;Sky Fracture;Onyx Blaster;Sand Shark Banner;Bone Biter Banner;Flesh Reaver Banner;Crystal Thresher Banner;Angry Tumbler Banner;Ancient Cloth;Desert Spirit Lamp;Music Box (Sandstorm);Apprentices Hat;Apprentices Robe;Apprentices Trousers;Squires Great Helm;Squires Plating;Squires Greaves;Huntresss Wig;Huntresss Jerkin;Huntresss Pants;Monks Bushy Brow Bald Cap;Monks Shirt;Monks Pants;Apprentices Scarf;Squires Shield;Huntresss Buckler;Monks Belt;Defenders Forge;War Table;War Table Banner;Eternia Crystal Stand;Defender Medal;Flameburst Rod;Flameburst Cane;Flameburst Staff;Ale Tosser;Etherian Mana;Brand of the Inferno;Ballista Rod;Ballista Cane;Ballista Staff;Flying Dragon;Eternia Crystal;Lightning Aura Rod;Lightning Aura Cane;Lightning Aura Staff;Explosive Trap Rod;Explosive Trap Cane;Explosive Trap Staff;Sleepy Octopod;Ghastly Glaive;Etherian Goblin Bomber Banner;Etherian Goblin Banner;Old Ones Skeleton Banner;Drakin Banner;Kobold Glider Banner;Kobold Banner;Wither Beast Banner;Etherian Wyvern Banner;Etherian Javelin Thrower Banner;Etherian Lightning Bug Banner;;;;;;Tome of Infinite Wisdom;ItemName.BoringBow;Phantom Phoenix;Gato Egg;Creeper Egg;Dragon Egg;Sky Dragons Fury;Aerial Bane;Treasure Bag (Betsy);;;Betsy Mask;Dark Mage Mask;Ogre Mask;Betsy Trophy;Dark Mage Trophy;Ogre Trophy;Music Box (Old Ones Army);Betsys Wrath;Valhalla Knights Helm;Valhalla Knights Breastplate;Valhalla Knights Greaves;Dark Artists Hat;Dark Artists Robes;Dark Artists Leggings;Red Riding Hood;Red Riding Dress;Red Riding Leggings;Shinobi Infiltrators Helmet;Shinobi Infiltrators Torso;Shinobi Infiltrators Pants;Betsys Wings;Crystal Chest;Golden Chest;Trapped Crystal Chest;Trapped Golden Chest;Crystal Door;Crystal Chair;Crystal Candle;Crystal Lantern;Crystal Lamp;Crystal Candelabra;Crystal Chandelier;Crystal Bathtub;Crystal Sink;Crystal Bed;Crystal Clock;Sunplate Clock;Blue Dungeon Clock;Green Dungeon Clock;Pink Dungeon Clock;Crystal Platform;Golden Platform;Dynasty Wood Platform;Lihzahrd Platform;Flesh Platform;Frozen Platform;Crystal Work Bench;Golden Work Bench;Crystal Dresser;Dynasty Dresser;Frozen Dresser;Living Wood Dresser;Crystal Piano;Dynasty Piano;Crystal Bookcase;Crystal Sofa;Dynasty Sofa;Crystal Table;Arkhalis Hood;Arkhalis Bodice;Arkhalis Tights;Arkhalis Lightwings;Leinfors Hair Protector;Leinfors Excessive Style;Leinfors Fancypants;Leinfors Prehensile Cloak;Leinfors Luxury Shampoo;Celebration Mk2;Spider Bathtub;Spider Bed;Spider Bookcase;Spider Dresser;Spider Candelabra;Spider Candle;Spider Chair;Spider Chandelier;Spider Chest;Spider Clock;Spider Door;Spider Lamp;Spider Lantern;Spider Piano;Spider Platform;Spider Sink;Spider Sofa;Spider Table;Spider Work Bench;Trapped Spider Chest;Iron Brick;Iron Brick Wall;Lead Brick;Lead Brick Wall;Lesion Block;Lesion Block Wall;Lesion Platform;Lesion Bathtub;Lesion Bed;Lesion Bookcase;Lesion Candelabra;Lesion Candle;Lesion Chair;Lesion Chandelier;Lesion Chest;Lesion Clock;Lesion Door;Lesion Dresser;Lesion Lamp;Lesion Lantern;Lesion Piano;Lesion Sink;Lesion Sofa;Lesion Table;Lesion Work Bench;Trapped Lesion Chest;Hat Rack;;Pearlwood Crate;Mythril Crate;Titanium Crate;Defiled Crate;Hematic Crate;Stockade Crate;Azure Crate;Divine Crate;Bramble Crate;Dead Mans Chest;Golf Ball;Amphibian Boots;Arcane Flower;Berserkers Glove;Fairy Boots;Frog Flipper;Frog Gear;Frog Webbing;Frozen Shield;Hero Shield;Magma Skull;Magnet Flower;Mana Cloak;Molten Quiver;Molten Skull Rose;Obsidian Skull Rose;Recon Scope;Stalkers Quiver;Stinger Necklace;Ultrabright Helmet;Apple;;Apple Pie;Banana Split;BBQ Ribs;Bunny Stew;Burger;Chicken Nugget;Chocolate Chip Cookie;Cream Soda;Escargot;Fried Egg;Fries;Golden Delight;Grapes;Grilled Squirrel;Hotdog;Ice Cream;Milkshake;Nachos;Pizza;Potato Chips;Roasted Bird;Roasted Duck;Sauteed Frog Legs;Seafood Dinner;Shrimp Po Boy;Spaghetti;Steak;Molten Charm;Golf Club (Iron);Golf Cup;Blue Flower Seeds;Magenta Flower Seeds;Pink Flower Seeds;Red Flower Seeds;Yellow Flower Seeds;Violet Flower Seeds;White Flower Seeds;Tall Grass Seeds;Lawn Mower;Crimstone Brick;Smooth Sandstone;Crimstone Brick Wall;Smooth Sandstone Wall;Blood Moon Monolith;Dunerider Boots;Ancient Chisel;Rain Song;;Fossil Pickaxe;Super Star Shooter;Storm Spear;Thunder Zapper;Drum Set;Picnic Table;Fancy Picnic Table;Desert Minecart;Minecarp;Pink Fairy;Green Fairy;Blue Fairy;Junonia Shell;Lightning Whelk Shell;Tulip Shell;Pin Wheel;Weather Vane;Void Vault;Music Box (Ocean Night);Music Box (Slime Rain);Music Box (Space Day);Music Box (Town Day);Music Box (Town Night);Music Box (Windy Day);White Pin Flag;Red Pin Flag;Green Pin Flag;Blue Pin Flag;Yellow Pin Flag;Purple Pin Flag;Golf Tee;Shell Pile;Anti-Portal Block;Golf Club (Putter);Golf Club (Wedge);Golf Club (Driver);Golf Whistle;Ebonwood Toilet;Rich Mahogany Toilet;Pearlwood Toilet;Living Wood Toilet;Cactus Toilet;Bone Toilet;Flesh Toilet;Mushroom Toilet;Skyware Toilet;Shadewood Toilet;Lihzahrd Toilet;Blue Dungeon Toilet;Green Dungeon Toilet;Pink Dungeon Toilet;Obsidian Toilet;Frozen Toilet;Glass Toilet;Honey Toilet;Steampunk Toilet;Pumpkin Toilet;Spooky Toilet;Dynasty Toilet;Palm Wood Toilet;Boreal Wood Toilet;Slime Toilet;Martian Toilet;Granite Toilet;Marble Toilet;Crystal Toilet;Spider Toilet;Lesion Toilet;Diamond Toilet;Maid Bonnet;Maid Dress;Maid Shoes;Void Bag;Pink Maid Bonnet;Pink Maid Dress;Pink Maid Shoes;Country Club Cap;Country Club Vest;Country Club Trousers;Country Club Visor;Spider Nest Block;Spider Nest Wall;Meteorite Toilet;Decay Chamber;ManaCloakStar;Terragrim;Solar Bathtub;Solar Bed;Solar Bookcase;Solar Dresser;Solar Candelabra;Solar Candle;Solar Chair;Solar Chandelier;Solar Chest;Solar Clock;Solar Door;Solar Lamp;Solar Lantern;Solar Piano;Solar Platform;Solar Sink;Solar Sofa;Solar Table;Solar Work Bench;Trapped Solar Chest;Solar Toilet;Vortex Bathtub;Vortex Bed;Vortex Bookcase;Vortex Dresser;Vortex Candelabra;Vortex Candle;Vortex Chair;Vortex Chandelier;Vortex Chest;Vortex Clock;Vortex Door;Vortex Lamp;Vortex Lantern;Vortex Piano;Vortex Platform;Vortex Sink;Vortex Sofa;Vortex Table;Vortex Work Bench;Trapped Vortex Chest;Vortex Toilet;Nebula Bathtub;Nebula Bed;Nebula Bookcase;Nebula Dresser;Nebula Candelabra;Nebula Candle;Nebula Chair;Nebula Chandelier;Nebula Chest;Nebula Clock;Nebula Door;Nebula Lamp;Nebula Lantern;Nebula Piano;Nebula Platform;Nebula Sink;Nebula Sofa;Nebula Table;Nebula Work Bench;Trapped Nebula Chest;Nebula Toilet;Stardust Bathtub;Stardust Bed;Stardust Bookcase;Stardust Dresser;Stardust Candelabra;Stardust Candle;Stardust Chair;Stardust Chandelier;Stardust Chest;Stardust Clock;Stardust Door;Stardust Lamp;Stardust Lantern;Stardust Piano;Stardust Platform;Stardust Sink;Stardust Sofa;Stardust Table;Stardust Work Bench;Trapped Stardust Chest;Stardust Toilet;Solar Brick;Vortex Brick;Nebula Brick;Stardust Brick;Solar Brick Wall;Vortex Brick Wall;Nebula Brick Wall;Stardust Brick Wall;Music Box (Day Remix);Cracked Blue Brick;Cracked Green Brick;Cracked Pink Brick;Wild Flower Seeds;Black Golf Ball;Blue Golf Ball;Brown Golf Ball;Cyan Golf Ball;Green Golf Ball;Lime Golf Ball;Orange Golf Ball;Pink Golf Ball;Purple Golf Ball;Red Golf Ball;Sky Blue Golf Ball;Teal Golf Ball;Violet Golf Ball;Yellow Golf Ball;Amber Robe;Amber Hook;Orange Phaseblade;Orange Phasesaber;Orange Stained Glass;Orange Pressure Plate;Snake Charmers Flute;Magic Conch;Golf Cart Keys;Golf Chest;Trapped Golf Chest;Sandstone Chest;Trapped Sandstone Chest;Sanguine Staff;Blood Thorn;Bloody Tear;Drippler Crippler;Vampire Frog Staff;Gold Goldfish;Gold Fish Bowl;Bast Statue;Gold Starry Block;Blue Starry Block;Gold Starry Wall;Blue Starry Wall;Finch Staff;Apricot;Banana;Blackcurrant;Blood Orange;Cherry;Coconut;Dragon Fruit;Elderberry;Grapefruit;Lemon;Mango;Peach;Pineapple;Plum;Rambutan;Star Fruit;Sandstone Bathtub;Sandstone Bed;Sandstone Bookcase;Sandstone Dresser;Sandstone Candelabra;Sandstone Candle;Sandstone Chair;Sandstone Chandelier;Sandstone Clock;Sandstone Door;Sandstone Lamp;Sandstone Lantern;Sandstone Piano;Sandstone Platform;Sandstone Sink;Sandstone Sofa;Sandstone Table;Sandstone Work Bench;Sandstone Toilet;Haemorrhaxe;Void Monolith;Arrow Sign;Painted Arrow Sign;Master Gamers Jacket;Master Gamers Pants;Star Princess Crown;Star Princess Dress;Chum Caster;Plate;Black Dragonfly Jar;Blue Dragonfly Jar;Green Dragonfly Jar;Orange Dragonfly Jar;Red Dragonfly Jar;Yellow Dragonfly Jar;Gold Dragonfly Jar;Black Dragonfly;Blue Dragonfly;Green Dragonfly;Orange Dragonfly;Red Dragonfly;Yellow Dragonfly;Gold Dragonfly;Step Stool;Dragonfly Statue;Paper Airplane;White Paper Airplane;Can Of Worms;Encumbering Stone;Gray Zapinator;Orange Zapinator;Green Moss;Brown Moss;Red Moss;Blue Moss;Purple Moss;Lava Moss;Boulder Statue;Music Box (Journeys Beginning);Music Box (Storm);Music Box (Graveyard);Seagull;Seagull Statue;Ladybug;Gold Ladybug;Maggot;Maggot Cage;Celestial Wand;Eucalyptus Sap;Blue Kite;Blue and Yellow Kite;Red Kite;Red and Yellow Kite;Yellow Kite;Ivy;Pupfish;Grebe;Rat;Rat Cage;Krypton Moss;Xenon Moss;Wyvern Kite;Ladybug Cage;Blood Rain Bow;Advanced Combat Techniques;Desert Torch;Coral Torch;Corrupt Torch;Crimson Torch;Hallowed Torch;Jungle Torch;Argon Moss;Rolling Cactus;Thin Ice;Echo Block;Scarab Fish;Scorpio Fish;Owl;Owl Cage;Owl Statue;Pupfish Bowl;Gold Ladybug Cage;Geode;Flounder;Rock Lobster;Lobster Tail;Inner Tube;Frozen Crate;Boreal Crate;Oasis Crate;Mirage Crate;Spectre Goggles;Oyster;Shucked Oyster;White Pearl;Black Pearl;Pink Pearl;Stone Door;Stone Platform;Oasis Water Fountain;Water Strider;Gold Water Strider;Lawn Flamingo;Music Box (Underground Jungle);Grate;Scarab Bomb;Wrought Iron Fence;Shark Bait;Bee Minecart;Ladybug Minecart;Pigron Minecart;Sunflower Minecart;Potted Forest Cedar;Potted Jungle Cedar;Potted Hallow Cedar;Potted Forest Tree;Potted Jungle Tree;Potted Hallow Tree;Potted Forest Palm;Potted Jungle Palm;Potted Hallow Palm;Potted Forest Bamboo;Potted Jungle Bamboo;Potted Hallow Bamboo;Scarab Fishing Rod;Demonic Hellcart;Witchs Broom;Cluster Rocket I;Cluster Rocket II;Wet Rocket;Lava Rocket;Honey Rocket;Shroom Minecart;Amethyst Minecart;Topaz Minecart;Sapphire Minecart;Emerald Minecart;Ruby Minecart;Diamond Minecart;Mini Nuke I;Mini Nuke II;Dry Rocket;Sandcastle Bucket;Turtle Cage;Jungle Turtle Cage;Gladius;Turtle;Jungle Turtle;Turtle Statue;Amber Minecart;Beetle Minecart;Meowmere Minecart;Party Wagon;The Dutchman;Steampunk Minecart;Grebe Cage;Seagull Cage;Water Strider Cage;Gold Water Strider Cage;Lesser Luck Potion;Luck Potion;Greater Luck Potion;Seahorse;Seahorse Cage;Gold Seahorse;Gold Seahorse Cage;1/2 Second Timer;1/4 Second Timer;Ebonstone Wall;Mud Wall;Pearlstone Wall;Snow Wall;Amethyst Stone Wall;Topaz Stone Wall;Sapphire Stone Wall;Emerald Stone Wall;Ruby Stone Wall;Diamond Stone Wall;Green Mossy Wall;Brown Mossy Wall;Red Mossy Wall;Blue Mossy Wall;Purple Mossy Wall;Rocky Dirt Wall;Old Stone Wall;Spider Wall;Corrupt Grass Wall;Hallowed Grass Wall;Ice Wall;Obsidian Wall;Crimson Grass Wall;Crimstone Wall;Cave Dirt Wall;Rough Dirt Wall;Craggy Stone Wall;Corrupt Growth Wall;Corrupt Mass Wall;Corrupt Pustule Wall;Corrupt Tendril Wall;Crimson Crust Wall;Crimson Scab Wall;Crimson Teeth Wall;Crimson Blister Wall;Layered Dirt Wall;Crumbling Dirt Wall;Cracked Dirt Wall;Wavy Dirt Wall;Hallowed Prism Wall;Hallowed Cavern Wall;Hallowed Shard Wall;Hallowed Crystalline Wall;Lichen Stone Wall;Leafy Jungle Wall;Ivy Stone Wall;Jungle Vine Wall;Ember Wall;Cinder Wall;Magma Wall;Smouldering Stone Wall;Worn Stone Wall;Stalactite Stone Wall;Mottled Stone Wall;Fractured Stone Wall;The Bride Banner;Zombie Merman Banner;Wandering Eye Fish Banner;Blood Squid Banner;Blood Eel Banner;Hemogoblin Shark Banner;Large Bamboo;Large Bamboo Wall;Demon Horns;Bamboo Leaf;Slice of Hell Cake;Fog Machine;Plasma Lamp;Marble Column;Chef Hat;Chef Uniform;Chef Pants;Star Hairpin;Heart Hairpin;Bunny Ears;Devil Horns;Fedora;Fake Unicorn Horn;Bamboo;Bamboo Wall;Bamboo Bathtub;Bamboo Bed;Bamboo Bookcase;Bamboo Dresser;Bamboo Candelabra;Bamboo Candle;Bamboo Chair;Bamboo Chandelier;Bamboo Chest;Bamboo Clock;Bamboo Door;Bamboo Lamp;Bamboo Lantern;Bamboo Piano;Bamboo Platform;Bamboo Sink;Bamboo Sofa;Bamboo Table;Bamboo Work Bench;Trapped Bamboo Chest;Bamboo Toilet;Worn Golf Club (Iron);Worn Golf Club (Putter);Worn Golf Club (Wedge);Worn Golf Club (Driver);Fancy Golf Club (Iron);Fancy Golf Club (Putter);Fancy Golf Club (Wedge);Fancy Golf Club (Driver);Premium Golf Club (Iron);Premium Golf Club (Putter);Premium Golf Club (Wedge);Premium Golf Club (Driver);Bronze Golf Trophy;Silver Golf Trophy;Gold Golf Trophy;Dreadnautilus Banner;Birdie Rattle;Exotic Chew Toy;Bedazzled Nectar;Music Box (Jungle Night);Desert Tiger Staff;Chum Bucket;Garden Gnome;Bone Serpent Kite;World Feeder Kite;Bunny Kite;Pigron Kite;Apple Juice;Grape Juice;Lemonade;Frozen Banana Daiquiri;Peach Sangria;Pi\u00f1a Colada;Tropical Smoothie;Bloody Moscato;Smoothie of Darkness;Prismatic Punch;Fruit Juice;Fruit Salad;Andrew Sphinx;Watchful Antlion;Burning Spirit;Jaws of Death;The Sands of Slime;Snakes, I Hate Snakes;Life Above the Sand;Oasis;Prehistory Preserved;Ancient Tablet;Uluru;Visiting the Pyramids;Bandage Boy;Divine Eye;Amethyst Stone Block;Topaz Stone Block;Sapphire Stone Block;Emerald Stone Block;Ruby Stone Block;Diamond Stone Block;Amber Stone Block;Amber Stone Wall;Man Eater Kite;Blue Jellyfish Kite;Pink Jellyfish Kite;Shark Kite;Superhero Mask;Superhero Costume;Superhero Tights;Pink Fairy Jar;Green Fairy Jar;Blue Fairy Jar;The Rolling Greens;Study of a Ball at Rest;Fore!;The Duplicity of Reflections;Fogbound Dye;Bloodbath Dye;Pretty Pink Dress;Pretty Pink Stockings;Pretty Pink Ribbon;Bamboo Fence;Illuminant Coating;Sand Shark Kite;Corrupt Bunny Kite;Vicious Bunny Kite;Leather Whip;Drumstick;Goldfish Kite;Angry Trapper Kite;Koi Kite;Crawltipede Kite;Durendal;Morning Star;Dark Harvest;Spectrum Kite;Release Doves;Wandering Eye Kite;Unicorn Kite;Gravedigger Hat;Gravedigger Coat;Angry Dandelion Banner;Gnome Banner;Desert Campfire;Coral Campfire;Corrupt Campfire;Crimson Campfire;Hallowed Campfire;Jungle Campfire;Soul of Light in a Bottle;Soul of Night in a Bottle;Soul of Flight in a Bottle;Soul of Sight in a Bottle;Soul of Might in a Bottle;Soul of Fright in a Bottle;Mud Bud;Release Lantern;Quad-Barrel Shotgun;Funeral Hat;Funeral Coat;Funeral Pants;Tragic Umbrella;Victorian Goth Hat;Victorian Goth Dress;Tattered Wood Sign;Gravediggers Shovel;Desert Chest;Trapped Desert Chest;Desert Key;Stellar Tune;Mollusk Whistle;Boreal Beam;Rich Mahogany Beam;Granite Column;Sandstone Column;Mushroom Beam;;Nevermore;Reborn;Graveyard;Ghost Manifestation;Wicked Undead;Bloody Goblet;Still Life;Ghostars Infinity Eight;Terra Toilet;Ghostars Soul Jar;Ghostars Garb;Ghostars Tights;Ball O Fuse Wire;Full Moon Squeaky Toy;Ornate Shadow Key;Dr. Man Fly Mask;Dr. Man Flys Lab Coat;Butcher Mask;Butchers Bloodstained Apron;Butchers Bloodstained Pants;Football;Hunter Cloak;Coffin Minecart;Safemans Blanket Cape;Safemans Sunny Day;Safemans Sun Dress;Safemans Pink Leggings;FoodBarbarians Tattered Dragon Wings;FoodBarbarians Horned Helm;FoodBarbarians Wild Wolf Spaulders;FoodBarbarians Savage Greaves;Grox The Greats Wings;Grox The Greats Horned Cowl;Grox The Greats Chestplate;Grox The Greats Greaves;Blade Staff;Squirrel Hook;Sergeant United Shield;Rock Golem Head;Critter Shampoo;Digging Molecart;Shroomerang;Tree Globe;World Globe;Guide to Critter Companionship;Dog Ears;Dog Tail;Fox Ears;Fox Tail;Lizard Ears;Lizard Tail;Panda Ears;Bunny Tail;Fairy Glowstick;Lightning Carrot;Prismatic Dye;Mushroom Hat;Mushroom Vest;Mushroom Pants;Treasure Bag (Empress of Light);Empress of Light Trophy;Empress of Light Mask;Dusty Rawhide Saddle;Royal Gilded Saddle;Black Studded Saddle;Jousting Lance;Shadow Jousting Lance;Hallowed Jousting Lance;Pogo Stick;The Black Spot;Hexxed Branch;Toy Tank;Goat Skull;Dark Mages Tome;Royal Delight;Suspicious Grinning Eye;Writhing Remains;Brain in a Jar;Possessed Skull;Sparkling Honey;Deactivated Probe;Pair of Eyeballs;Robotic Skull;Plantera Seedling;Guardian Golem;Pork of the Sea;Tablet Fragment;Piece of Moon Squid;Jewel of Light;Pumpkin Scented Candle;Shrub Star;Frozen Crown;Cosmic Skateboard;Ogres Club;Betsys Egg;Combat Wrench;Demon Conch;Bottomless Lava Bucket;Lavaproof Bug Net;Flame Waker Boots;Empress Wings;Wet Bomb;Lava Bomb;Honey Bomb;Dry Bomb;Superheated Blood;Cat License;Dog License;Amethyst Squirrel;Topaz Squirrel;Sapphire Squirrel;Emerald Squirrel;Ruby Squirrel;Diamond Squirrel;Amber Squirrel;Amethyst Bunny;Topaz Bunny;Sapphire Bunny;Emerald Bunny;Ruby Bunny;Diamond Bunny;Amber Bunny;Hell Butterfly;Hell Butterfly Jar;Lavafly;Lavafly in a Bottle;Magma Snail;Magma Snail Cage;Topaz Gemcorn;Amethyst Gemcorn;Sapphire Gemcorn;Emerald Gemcorn;Ruby Gemcorn;Diamond Gemcorn;Amber Gemcorn;Hanging Pot;Hanging Daybloom;Hanging Moonglow;Hanging Waterleaf;Hanging Shiverthorn;Hanging Blinkroot;Hanging Corrupt Deathweed;Hanging Crimson Deathweed;Hanging Fireblossom;Hanging Brazier;Mini Volcano;Large Volcano;Potion of Return;Sakura Sapling;Lava Absorbant Sponge;Hallowed Hood;Hellfire Treads;Jungle Pylon;Forest Pylon;Obsidian Crate;Hellstone Crate;Obsidian Lock Box;Lava Serpent Bowl;Lavaproof Fishing Hook;Amethyst Bunny Cage;Topaz Bunny Cage;Sapphire Bunny Cage;Emerald Bunny Cage;Ruby Bunny Cage;Diamond Bunny Cage;Amber Bunny Cage;Amethyst Squirrel Cage;Topaz Squirrel Cage;Sapphire Squirrel Cage;Emerald Squirrel Cage;Ruby Squirrel Cage;Diamond Squirrel Cage;Amber Squirrel Cage;Ancient Hallowed Mask;Ancient Hallowed Helmet;Ancient Hallowed Headgear;Ancient Hallowed Hood;Ancient Hallowed Plate Mail;Ancient Hallowed Greaves;Potted Magma Palm;Potted Brimstone Bush;Potted Fire Brambles;Potted Lava Bulb;Potted Ember Tendrils;Yellow Willow Sapling;Dirt Bomb;Sticky Dirt Bomb;Bunny License;Cool Whip;Firecracker;Snapthorn;Kaleidoscope;Tungsten Bullet;Hallow Pylon;Cavern Pylon;Ocean Pylon;Desert Pylon;Snow Pylon;Mushroom Pylon;Cavern Water Fountain;Starlight;Eye of Cthulhu Relic;Eater of Worlds Relic;Brain of Cthulhu Relic;Skeletron Relic;Queen Bee Relic;King Slime Relic;Wall of Flesh Relic;Twins Relic;Destroyer Relic;Skeletron Prime Relic;Plantera Relic;Golem Relic;Duke Fishron Relic;Lunatic Cultist Relic;Moon Lord Relic;Martian Saucer Relic;Flying Dutchman Relic;Mourning Wood Relic;Pumpking Relic;Ice Queen Relic;Everscream Relic;Santa-NK1 Relic;Dark Mage Relic;Ogre Relic;Betsy Relic;Empress of Light Relic;Queen Slime Relic;Universal Pylon;Nightglow;Eventide;Celestial Starboard;Rabbit Perch;Zenith;Treasure Bag (Queen Slime);Queen Slime Trophy;Queen Slime Mask;Regal Delicacy;Prismatic Lacewing;Stone Accent Slab;Truffle Worm Cage;Prismatic Lacewing Jar;Rock Golem Banner;Blood Mummy Banner;Spore Skeleton Banner;Spore Bat Banner;Antlion Larva Banner;Vicious Bunny Banner;Vicious Goldfish Banner;Vicious Penguin Banner;Corrupt Mimic Banner;Crimson Mimic Banner;Hallowed Mimic Banner;Moss Hornet Banner;Wandering Eye Banner;Fledgling Wings;Music Box (Queen Slime);Hook of Dissonance;Gelatinous Pillion;Crystal Assassin Hood;Crystal Assassin Shirt;Crystal Assassin Pants;Music Box (Empress Of Light);Sparkle Slime Balloon;Volatile Gelatin;Gelatin Crystal;Soaring Insignia;Music Box (Duke Fishron);Music Box (Morning Rain);Music Box (Alt Title);Chippys Couch;Blue Graduation Cap;Maroon Graduation Cap;Black Graduation Cap;Blue Graduation Gown;Maroon Graduation Gown;Black Graduation Gown;Terraspark Boots;Moon Lord Legs;Ocean Crate;Seaside Crate;Badgers Hat;Terraprisma;Music Box (Underground Desert);Dead Mans Sweater;Teapot;Teacup;Treasure Magnet;Mace;Flaming Mace;ItemName.SleepingIcon;Otherworldly Music Box (Rain);Otherworldly Music Box (Overworld Day);Otherworldly Music Box (Night);Otherworldly Music Box (Underground);Otherworldly Music Box (Desert);Otherworldly Music Box (Ocean);Otherworldly Music Box (Mushrooms);Otherworldly Music Box (Dungeon);Otherworldly Music Box (Space);Otherworldly Music Box (Underworld);Otherworldly Music Box (Snow);Otherworldly Music Box (Corruption);Otherworldly Music Box (Underground Corruption);Otherworldly Music Box (Crimson);Otherworldly Music Box (Underground Crimson);Otherworldly Music Box (Ice);Otherworldly Music Box (Underground Hallow);Otherworldly Music Box (Eerie);Otherworldly Music Box (Boss 2);Otherworldly Music Box (Boss 1);Otherworldly Music Box (Invasion);Otherworldly Music Box (The Towers);Otherworldly Music Box (Lunar Boss);Otherworldly Music Box (Plantera);Otherworldly Music Box (Jungle);Otherworldly Music Box (Wall of Flesh);Otherworldly Music Box (Hallow);Carton of Milk;Coffee;Torch Gods Favor;Music Box (Journeys End);Plaguebringers Skull;Plaguebringers Cloak;Plaguebringers Treads;Wandering Jingasa;Wandering Yukata;Wandering Geta;Timeless Travelers Hood;Timeless Travelers Cloak;Timeless Travelers Footwear;Floret Protector Helmet;Floret Protector Shirt;Floret Protector Pants;Capricorn Helmet;Capricorn Chestplate;Capricorn Hooves;Capricorn Tail;Video Visage;Lazer Blazer;Pinstripe Pants;Lavaproof Tackle Bag;Resonance Scepter;Bee Hive;Antlion Eggs;Flinx Fur Coat;Flinx Staff;Flinx Fur;Royal Tiara;Royal Blouse;Royal Dress;Spinal Tap;Rainbow Cursor;Royal Scepter;Glass Slipper;Prince Uniform;Prince Pants;Prince Cape;Potted Crystal Fern;Potted Crystal Spiral;Potted Crystal Teardrop;Potted Crystal Tree;Princess 64;Painting of a Lass;Dark Side of the Hallow;Bernies Button;Glommers Flower;Deerclops Eyeball;Monster Meat;Monster Lasagna;Froggle Bunwich;Tentacle Spike;Lucy the Axe;Ham Bat;Bat Bat;Eye Bone;Garland;Bone Helm;Eyebrella;Gentlemans Vest;Gentlemans Trousers;Gentlemans Beard;Gentlemans Long Beard;Gentlemans Magnificent Beard;Magiluminescence;Deerclops Trophy;Deerclops Mask;Deerclops Relic;Treasure Bag (Deerclops);Music Box (Deerclops);Radio Thing;Abigails Flower;Firestarters Sweater;Firestarters Skirt;Pew-matic Horn;Weather Pain;Houndius Shootius;Deer Thing;The Gentleman Scientist;The Firestarter;The Bereaved;The Strongman;Fart Kart;Hand Of Creation;Neon Moss;Helium Moss;Flymeal;Liliths Necklace;Resplendent Dessert;Stinkbug;Stinkbug Cage;Terraformer;Venom Dart Trap;Vulkelf Ears;Stinkbug Blocker;Ghostly Stinkbug Blocker;Fishing Bobber;Glowing Fishing Bobber;Lava Moss Fishing Bobber;Krypton Moss Fishing Bobber;Xenon Moss Fishing Bobber;Argon Moss Fishing Bobber;Neon Moss Fishing Bobber;Helium Moss Fishing Bobber;Wand of Frosting;Reef Bathtub;Reef Bed;Reef Bookcase;Reef Dresser;Reef Candelabra;Reef Candle;Reef Chair;Reef Chandelier;Reef Chest;Reef Clock;Reef Door;Reef Lamp;Reef Lantern;Reef Piano;Reef Platform;Reef Sink;Reef Sofa;Reef Table;Reef Work Bench;Trapped Reef Chest;Reef Toilet;Balloon Bathtub;Balloon Bed;Balloon Bookcase;Balloon Dresser;Balloon Candelabra;Balloon Candle;Balloon Chair;Balloon Chandelier;Balloon Chest;Balloon Clock;Balloon Door;Balloon Lamp;Balloon Lantern;Balloon Piano;Balloon Platform;Balloon Sink;Balloon Sofa;Balloon Table;Balloon Work Bench;Trapped Balloon Chest;Balloon Toilet;Ash Wood Bathtub;Ash Wood Bed;Ash Wood Bookcase;Ash Wood Dresser;Ash Wood Candelabra;Ash Wood Candle;Ash Wood Chair;Ash Wood Chandelier;Ash Wood Chest;Ash Wood Clock;Ash Wood Door;Ash Wood Lamp;Ash Wood Lantern;Ash Wood Piano;Ash Wood Platform;Ash Wood Sink;Ash Wood Sofa;Ash Wood Table;Ash Wood Work Bench;Trapped Ash Wood Chest;Ash Wood Toilet;Biome Sight Potion;Scarlet Macaw;Scarlet Macaw Cage;Ash Grass Seeds;Ash Wood;Ash Wood Wall;Ash Wood Fence;Outcast;Fairy Guides;A Horrible Night for Alchemy;Morning Hunt;Suspiciously Sparkly;Requiem;Cat Sword;Kargohs Summon;High Pitch;A Machine for Terrarians;Terra Blade Chronicles;Benny Warhol;Lizard King;My Son;Duality;Parsec Pals;Remnants of Devotion;Not So Lost In Paradise;Ocular Resonance;Wings of Evil;Constellation;Eyezorhead;Dread of the Red Sea;Do Not Eat the Vile Mushroom!;Yuuma, The Blue Tiger;Moonman & Company;Sunshine of Israpony;Purity;Sufficiently Advanced;Strange Growth;Happy Little Tree;Strange Dead Fellows;Secrets;Thunderbolt;Crustography;The Werewolf;Blessing from the Heavens;Love is in the Trash Slot;Fangs;Hail to the King;See The World For What It Is;What Lurks Below;This Is Getting Out Of Hand;Buddies;Midnight Sun;Couch Gag;Silent Fish;The Duke;Royal Romance;Bioluminescence;Wildflowers;Viking Voyage;Bifrost;Heartlands;Forest Troll;Aurora Borealis;Lady Of The Lake;Joja Cola;Stardrop;Spicy Pepper;Pomegranate;Ash Wood Helmet;Ash Wood Breastplate;Ash Wood Greaves;Ash Wood Bow;Ash Wood Hammer;Ash Wood Sword;Moon Globe;Repaired Life Crystal;Repaired Mana Crystal;Terra Fart Kart;Minecart Upgrade Kit;Jims Cap;Echo Wall;Echo Platform;Mushroom Torch;Hive-Five;Axe of Regrowth;Chlorophyte Extractinator;Blue Chicken Egg;Trimarang;Mushroom Campfire;Blue Macaw;Blue Macaw Cage;Bottomless Honey Bucket;Honey Absorbant Sponge;Ultra Absorbant Sponge;Goblorc Ears;Reef Block;Reef Wall;r/Terraria;Guide to Environmental Preservation;Princess Style;Toucan;Yellow Cockatiel;Gray Cockatiel;Toucan Cage;Yellow Cockatiel Cage;Gray Cockatiel Cage;Macaw Statue;Toucan Statue;Cockatiel Statue;Decorative Healing Potion;Decorative Mana Potion;Shadow Candle;Guide to Peaceful Coexistence;Rubblemaker (Small);Closed Void Bag;Artisan Loaf;TNT Barrel;Chest Lock;Rubblemaker (Medium);Rubblemaker (Large);Bundle of Horseshoe Balloons;Spiffo Plush;Glow Tulip;Ocrams Razor;Rod of Harmony;Advanced Combat Techniques: Volume Two;Vital Crystal;Aegis Fruit;Arcane Crystal;Galaxy Pearl;Gummy Worm;Ambrosia;Peddlers Satchel;Echo Coating;Echo Chamber;Gas Trap;Aether Monolith;Shimmer Arrow;Aetherium Block;Faeling;Faeling in a Bottle;Shimmer Slime Banner;Aether Torch;Reflective Shades;Chromatic Cloak;Used Gas Trap;Aether Campfire;Shellphone (Home);Shellphone (Spawn);Shellphone (Ocean);Shellphone (Underworld);Music Box (Aether);Infested Spider Wall;Bottomless Shimmer Bucket;Cursed Blue Brick Wall;Cursed Blue Slab Wall;Cursed Blue Tiled Wall;Cursed Pink Brick Wall;Cursed Pink Slab Wall;Cursed Pink Tiled Wall;Cursed Green Brick Wall;Cursed Green Slab Wall;Cursed Green Tiled Wall;Treacherous Sandstone Wall;Treacherous Hardened Sand Wall;Forbidden Lihzahrd Brick Wall;Spelunker Flare;Cursed Flare;Rainbow Flare;Shimmer Flare;Enchanted Moondial;Waffles Iron;Bouncy Boulder;Life Crystal Boulder;Dizzys Rare Gecko Chester;Raynbros Hoodie;Raynbros Pants;Eye of the Sun;Cheesy Pizza Poster;Raynbros Hood;Uncumbering Stone;Yellow Solution;White Solution;Brown Solution;Poo;Poo Wall;Aetherium Wall;Aetherium Brick;Aetherium Brick Wall;The Dirtiest Block;Lunar Rust Brick;Dark Celestial Brick;Astra Brick;Cosmic Ember Brick;Cryocore Brick;Mercury Brick;Star Royale Brick;Heavenforge Brick;Lunar Rust Brick Wall;Dark Celestial Brick Wall;Astra Brick Wall;Cosmic Ember Brick Wall;Cryocore Brick Wall;Mercury Brick Wall;Star Royale Brick Wall;Heavenforge Brick Wall;Ancient Blue Brick;Ancient Blue Brick Wall;Ancient Green Brick;Ancient Green Brick Wall;Ancient Pink Brick;Ancient Pink Brick Wall;Ancient Gold Brick;Ancient Gold Brick Wall;Ancient Silver Brick;Ancient Silver Brick Wall;Ancient Copper Brick;Ancient Copper Brick Wall;Ancient Cobalt Brick;Ancient Cobalt Brick Wall;Ancient Mythril Brick;Ancient Mythril Brick Wall;Ancient Obsidian Brick;Ancient Obsidian Brick Wall;Ancient Hellstone Brick;Ancient Hellstone Brick Wall;Shellphone;Fertilizer;Lava Moss Brick;Argon Moss Brick;Krypton Moss Brick;Xenon Moss Brick;Neon Moss Brick;Helium Moss Brick;Lava Moss Brick Wall;Argon Moss Brick Wall;Krypton Moss Brick Wall;Xenon Moss Brick Wall;Neon Moss Brick Wall;Helium Moss Brick Wall;Kwad Racer Drone;FPV Goggles;Guide to Critter Companionship (Inactive);Guide to Environmental Preservation (Inactive);Guide to Peaceful Coexistence (Inactive);Mushroom Staff;The Beheadeds Head;The Beheadeds Cuirass;The Beheadeds Trousers;Barrel Launcher;Killing Deck;Flint;Barnacle Staff;Mitey-Titey;Ram Rune;Swarm Grenade;Replica Demon Altar;Replica Crimson Altar;Replica Shadow Orb;Replica Crimson Heart;Decorative Cobweb;Item Flask;Cobwhip;Soulscourge;Vasculash;Starcrash;Vulgar Display of Flower;Electric Eel;Constellation;Possession;Portable Kiln;Alchemy Flask;Queen of Bees;Cow Bell;Chicken Charm;The Sea of Silence;Heroes From Another World;Crystallize;Eater Of Life;This Is Canon Now;Winter At Varingskollen;Magic Shimmer Dropper;Shimmerfall Block;Shimmerfall Wall;Shimmer Gun;Jungle Juice;Pink Banner;White Banner;Froggy Neckband;Goats Tuft;Old Companion Locket;Cat Chime;Dog Collar;Turkey Wattle Necklace;Mean Goblins Spikes;Crows Beak;Balloony Beads;Grim Old Barb;Vampire Pendant;Slashers Mysterious Skull;Pufferfish;Pufferfish Cage;Puffer Shrimp;Rainbow Boulder;Moon Lord Torso;Poo Boulder;Faecorn;Infused Fertilizer;Axearang;Lava Boulder;Spider Boulder;Ghoulder;Friendly Boulder;Chlorophyte Visor;Cursed Piper Flute;Flairon;Of Sea and Dreams;The Runic Pixie;Banner of the Beast;Stickman vs Terr Terr;Cozy Window;Demon Altar;Crimson Altar;Fairy Choker;Pink Phaseblade;Pink Phasesaber;Blackened Fish;Music Box (Queen Bee);Music Box (The Twins);Magic String;Magic Yoyo Bag;Freeze Bomb;Stress Ball;Cloud Platform;Overgrown Living Wood Wall;Natural Dirt Wall;Strung Counterweight;Aetherium Bathtub;Aetherium Bed;Aetherium Bookcase;Aetherium Dresser;Aetherium Candelabra;Aetherium Candle;Aetherium Chair;Aetherium Chandelier;Aetherium Chest;Aetherium Clock;Aetherium Door;Aetherium Lamp;Aetherium Lantern;Aetherium Piano;Aetherium Platform;Aetherium Sink;Aetherium Sofa;Aetherium Table;Aetherium Work Bench;Trapped Aetherium Chest;Aetherium Toilet;Lava Cloud;Star Cloud;Rainbow Cloud;Mud Ball;Torch Gods Flavor;Lucky Clover;Wilted Clover;Raven Feather;Pretty Mirror;Music Box (King Slime);Music Box (Alt Queen Bee);Music Box (Lunatic Cultist);Music Box (Skeletron Prime);Music Box (The Destroyer);Chicken Bones Visor;Chicken Bones Vest;Chicken Bones Pants;Chicken Bones Wings;Chicken Bones Robe;Prospector Helmet;Prospector Shirt;Prospector Pants;Captain Hat;Captain Vest;Captain Pants;Power Bomb;Sticky Power Bomb;Welding Mask;Amulet of the Night;CRT Monolith;Retro Monolith;Blue Roller Skates;Fallen Star Bathtub;Fallen Star Bed;Fallen Star Bookcase;Fallen Star Dresser;Fallen Star Candelabra;Fallen Star Candle;Fallen Star Chair;Fallen Star Chandelier;Fallen Star Chest;Fallen Star Clock;Fallen Star Door;Fallen Star Lamp;Fallen Star Lantern;Fallen Star Piano;Fallen Star Platform;Fallen Star Sink;Fallen Star Sofa;Fallen Star Table;Fallen Star Work Bench;Trapped Fallen Star Chest;Fallen Star Toilet;Fallen Star Block;Fallen Star Wall;Chippys Helmet;Chippys Chestplate;Chippys Greaves;Chippys Cloak;Chippys Headband;Acorn Slingshot;r/Terraria 2023;Bould and Bash;Dark Forebodings;Oktober;Its Scragglin Time;Kaguya;Prost;Music Box (Eater of Worlds);Music Box (Torch God);Music Box (Alt Torch God);Green Roller Skates;Classic Roller Skates;Party Roller Skates;Rainbow Glowstick;Scrying Orb;Rock Candy;Blue Bikini Top;Blue Bikini Bottom;Red Swimsuit;Green Swimshorts;Gray Swimshorts;Orca Banner;Underworld Pylon;Aether Pylon;Friendly Rainbow Boulder;Film Projector;Heroicis Hat;Heroicis Coat;Heroicis Pants;Heroicis Wings;Hallowed Crown;Heroicis Wings (Inactive);Enchanted Pixie Dust;Cattiva;Foxparks;Chillet;Chillet Ignis;Digtoise;The Imploder;True Copper Shortsword;Rainbow Phaseblade;Rainbow Phasesaber;Librarian Skeleton Banner;Water Bolt Mimic Banner;Dull Red Team Block;Dull Green Team Block;Dull Blue Team Block;Dull Yellow Team Block;Dull Pink Team Block;Dull White Team Block;Lilac Dusk Hairclip;Lilac Dusk Dress;Lilac Dusk Skirt;Kazzymodus Hood;Kazzymodus Chestpiece;Kazzymodus Leggings;Kazzymodus Wings;Slime Spear;Slime Whip;Feywood Bathtub;Feywood Bed;Feywood Bookcase;Feywood Dresser;Feywood Candelabra;Feywood Candle;Feywood Chair;Feywood Chandelier;Feywood Chest;Feywood Clock;Feywood Door;Feywood Lamp;Feywood Lantern;Feywood Piano;Feywood Platform;Feywood Sink;Feywood Sofa;Feywood Table;Feywood Work Bench;Trapped Feywood Chest;Feywood Toilet;Feywood;Feywood Wall;Hallowed Bathtub;Hallowed Bed;Hallowed Bookcase;Hallowed Dresser;Hallowed Candelabra;Hallowed Candle;Hallowed Chair;Hallowed Chandelier;Fancy Hallowed Chest;Hallowed Clock;Hallowed Door;Hallowed Lamp;Hallowed Lantern;Hallowed Piano;Hallowed Platform;Hallowed Sink;Hallowed Sofa;Hallowed Table;Hallowed Work Bench;Trapped Fancy Hallowed Chest;Hallowed Toilet;Hallowed Brick;Hallowed Brick Wall;Pal Metal Chestplate;Pal Metal Leggings;Chippys Cloak (Inactive);Wall Racer Car;Gothic Bathtub;Gothic Bed;Gothic Dresser;Gothic Candelabra;Gothic Candle;Gothic Chandelier;Gothic Chest;Gothic Clock;Gothic Door;Gothic Lamp;Gothic Lantern;Gothic Piano;Gothic Platform;Gothic Sink;Gothic Sofa;Trapped Gothic Chest;Gothic Toilet;Demonite Bathtub;Demonite Bed;Demonite Bookcase;Demonite Candelabra;Demonite Candle;Demonite Chair;Demonite Chandelier;Demonite Chest;Demonite Clock;Demonite Door;Demonite Dresser;Demonite Lamp;Demonite Lantern;Demonite Piano;Demonite Platform;Demonite Sink;Demonite Sofa;Demonite Table;Demonite Toilet;Demonite Work Bench;Trapped Demonite Chest;Crimtane Bathtub;Crimtane Bed;Crimtane Bookcase;Crimtane Candelabra;Crimtane Candle;Crimtane Chair;Crimtane Chandelier;Crimtane Chest;Crimtane Clock;Crimtane Door;Crimtane Dresser;Crimtane Lamp;Crimtane Lantern;Crimtane Piano;Crimtane Platform;Crimtane Sink;Crimtane Sofa;Crimtane Table;Crimtane Toilet;Crimtane Work Bench;Trapped Crimtane Chest;Snow Bathtub;Snow Bed;Snow Bookcase;Snow Candelabra;Snow Candle;Snow Chair;Snow Chandelier;Snow Chest;Snow Clock;Snow Door;Snow Dresser;Snow Lamp;Snow Lantern;Snow Piano;Snow Platform;Snow Sink;Snow Sofa;Snow Table;Snow Toilet;Snow Work Bench;Trapped Snow Chest;Flinx Fur Bathtub;Flinx Fur Bed;Flinx Fur Bookcase;Flinx Fur Candelabra;Flinx Fur Candle;Flinx Fur Chair;Flinx Fur Chandelier;Flinx Fur Chest;Flinx Fur Clock;Flinx Fur Door;Flinx Fur Dresser;Flinx Fur Lamp;Flinx Fur Lantern;Flinx Fur Piano;Flinx Fur Platform;Flinx Fur Sink;Flinx Fur Sofa;Flinx Fur Table;Flinx Fur Toilet;Flinx Fur Work Bench;Trapped Flinx Fur Chest;Pine Bathtub;Pine Bed;Pine Bookcase;Pine Candelabra;Pine Candle;Pine Chandelier;Pine Chest;Pine Clock;Pine Dresser;Pine Lamp;Pine Lantern;Pine Piano;Pine Platform;Pine Sink;Pine Sofa;Pine Toilet;Pine Work Bench;Trapped Pine Chest;Easter Bathtub;Easter Bed;Easter Bookcase;Easter Candelabra;Easter Candle;Easter Chair;Easter Chandelier;Easter Chest;Easter Clock;Easter Door;Easter Dresser;Easter Lamp;Easter Lantern;Easter Piano;Easter Platform;Easter Sink;Easter Sofa;Easter Table;Easter Toilet;Easter Work Bench;Trapped Easter Chest;Stone Bathtub;Stone Bed;Stone Bookcase;Stone Candelabra;Stone Candle;Stone Chair;Stone Chandelier;Stone Chest;Stone Clock;Stone Dresser;Stone Lamp;Stone Lantern;Stone Piano;Stone Sink;Stone Sofa;Stone Table;Stone Toilet;Stone Work Bench;Trapped Stone Chest;Jellyfish Bathtub;Jellyfish Bed;Jellyfish Bookcase;Jellyfish Candelabra;Jellyfish Candle;Jellyfish Chair;Jellyfish Chandelier;Jellyfish Chest;Jellyfish Clock;Jellyfish Door;Jellyfish Dresser;Jellyfish Lamp;Jellyfish Lantern;Jellyfish Piano;Jellyfish Platform;Jellyfish Sink;Jellyfish Sofa;Jellyfish Table;Jellyfish Toilet;Jellyfish Work Bench;Trapped Jellyfish Chest;Pine Tree Wall;Easter Block;Easter Wall;Gothic Brick;Gothic Brick Wall;Flinx Fur Block;Flinx Fur Wall;Jellyfish Block;Jellyfish Wall;Toybreaker Brick;Remix;Pine Wood;Pine Wood Wall;Harpy Bathtub;Harpy Bed;Harpy Bookcase;Harpy Candelabra;Harpy Candle;Harpy Chair;Harpy Chandelier;Harpy Chest;Harpy Clock;Harpy Door;Harpy Dresser;Harpy Lamp;Harpy Lantern;Harpy Piano;Harpy Platform;Harpy Sink;Harpy Sofa;Harpy Table;Harpy Toilet;Harpy Work Bench;Trapped Harpy Chest;Harpy Block;Harpy Wall;Cloud Bathtub;Cloud Bed;Cloud Bookcase;Cloud Candelabra;Cloud Candle;Cloud Chair;Cloud Chandelier;Cloud Chest;Cloud Clock;Cloud Door;Cloud Dresser;Cloud Lamp;Cloud Lantern;Cloud Piano;Cloud Sink;Cloud Sofa;Cloud Table;Cloud Toilet;Cloud Work Bench;Trapped Cloud Chest;Duskware Bathtub;Duskware Bed;Duskware Bookcase;Duskware Candelabra;Duskware Candle;Duskware Chair;Duskware Chandelier;Duskware Chest;Duskware Clock;Duskware Door;Duskware Dresser;Duskware Lamp;Duskware Lantern;Duskware Piano;Duskware Platform;Duskware Sink;Duskware Sofa;Duskware Table;Duskware Toilet;Duskware Work Bench;Trapped Duskware Chest;Moonplate Block;Crescent Wall;Librarian Bathtub;Librarian Bed;Librarian Bookcase;Librarian Candelabra;Librarian Candle;Librarian Chair;Librarian Chandelier;Librarian Chest;Librarian Clock;Librarian Door;Librarian Dresser;Librarian Lamp;Librarian Lantern;Librarian Piano;Librarian Platform;Librarian Sink;Librarian Sofa;Librarian Table;Librarian Toilet;Librarian Work Bench;Trapped Librarian Chest;Librarian Block;Librarian Wall;Spike Bathtub;Spike Bed;Spike Bookcase;Spike Candelabra;Spike Candle;Spike Chair;Spike Chandelier;Spike Chest;Spike Clock;Spike Door;Spike Dresser;Spike Lamp;Spike Lantern;Spike Piano;Spike Platform;Spike Sink;Spike Sofa;Spike Table;Spike Toilet;Spike Work Bench;Trapped Spike Chest;Spike Block;Spike Wall;Office Bathtub;Office Bed;Office Bookcase;Office Candelabra;Office Candle;Office Chair;Office Chandelier;Office Chest;Office Clock;Office Door;Office Dresser;Office Lamp;Office Lantern;Office Piano;Office Platform;Office Sink;Office Sofa;Office Table;Office Toilet;Office Work Bench;Trapped Office Chest;Office Block;Office Wall;Forbidden Bathtub;Forbidden Bed;Forbidden Bookcase;Forbidden Candelabra;Forbidden Candle;Forbidden Chair;Forbidden Chandelier;Forbidden Chest;Forbidden Clock;Forbidden Door;Forbidden Dresser;Forbidden Lamp;Forbidden Lantern;Forbidden Piano;Forbidden Platform;Forbidden Sink;Forbidden Sofa;Forbidden Table;Forbidden Toilet;Forbidden Work Bench;Trapped Forbidden Chest;Forbidden Block;Forbidden Wall;Aquarium Bathtub;Aquarium Bed;Aquarium Bookcase;Aquarium Candelabra;Aquarium Candle;Aquarium Chair;Aquarium Chandelier;Aquarium Clock;Aquarium Door;Aquarium Dresser;Aquarium Lamp;Aquarium Lantern;Aquarium Piano;Aquarium Platform;Aquarium Sink;Aquarium Sofa;Aquarium Table;Aquarium Toilet;Aquarium Work Bench;Aquarium Block;Aquarium Wall;Boulder Bathtub;Boulder Bed;Boulder Bookcase;Boulder Candelabra;Boulder Candle;Boulder Chair;Boulder Chandelier;Boulder Chest;Boulder Clock;Boulder Door;Boulder Dresser;Boulder Lamp;Boulder Lantern;Boulder Piano;Boulder Platform;Boulder Sink;Boulder Sofa;Boulder Table;Boulder Toilet;Boulder Work Bench;Trapped Boulder Chest;Boulder Block;Boulder Wall;Sharp Spike Block;{$ItemName.DemonAltar};{$ItemName.CrimsonAltar};Lunas Runic Pixie Hood;Lunas Runic Pixie Shirt;Lunas Runic Pixie Pants;Lunas Runic Pixie Wings;Lunas Runic Pixie Cloak;Huge Dragon Egg;ItemName.FoxparksTagEffect;Music Box (Skeletron);".split(";");
    za.pid = "None                                                                                                                         FieryGreatsword                                                                                                                                                                                                                                                                                                     MudstoneBlock                                                                                               Wrench                                                     MusicBoxOverworldDay MusicBoxEerie MusicBoxNight MusicBoxTitle MusicBoxUnderground MusicBoxBoss1 MusicBoxJungle MusicBoxCorruption MusicBoxUndergroundCorruption MusicBoxTheHallow MusicBoxBoss2 MusicBoxUndergroundHallow MusicBoxBoss3         Timer1Second Timer3Second Timer5Second                                                                                                IceChest                                                                                                                          EskimoHood EskimoCoat EskimoPants                  GhostWings                                                                                                                                                           PinkEskimoHood PinkEskimoCoat PinkEskimoPants               BlendOMatic                                     FlameAndBlackDye  GreenFlameAndBlackDye  BlueFlameAndBlackDye                                                                                  PurpleMucos                                                                                                                                                                                                                    Vertebrae                                                                DiablostLamp OilRagSconse                                                                                                                                         FrozenChest                     SDMG                           DTownsHelmet DTownsBreastplate DTownsLeggings DTownsWings             MusicBoxSnow MusicBoxSpace MusicBoxCrimson MusicBoxBoss4 MusicBoxAltOverworldDay MusicBoxRain MusicBoxIce MusicBoxDesert MusicBoxOcean MusicBoxDungeon MusicBoxPlantera MusicBoxBoss5 MusicBoxTemple MusicBoxEclipse MusicBoxMushrooms           BatBanner                      ZombieEskimoBanner                      JellyfishBanner           PirateBanner      SkeletonMageBanner SlimeBanner  SpiderBanner   TortoiseBanner      NypmhBanner                                                                               PrincessDressNew                                             SpookyPlatform                                             FartInABalloon                WhiteAndRedGarland RedGardland RedAndGreenGardland GreenGardland GreenAndWhiteGarland     RedAndGreenBulb YellowAndGreenBulb RedAndYellowBulb  WhiteAndRedBulb WhiteAndYellowBulb WhiteAndGreenBulb      RedAndYellowLights RedAndGreenLights YellowAndGreenLights BlueAndGreenLights RedAndBlueLights BlueAndYellowLights            CnadyCanePickaxe               MrsClauseHat MrsClauseShirt MrsClauseHeels                         BabyGrinchMischiefWhistle  SantaNK1Trophy  MusicBoxPumpkinMoon MusicBoxAltUnderground MusicBoxFrostMoon                        Womannquin                   FancyGreyWallpaper                                                                                                BrainMask FleshMask   BeeMask   EaterMask EyeMask                                                            HeavyWorkBench                     FleshCloningVaat                               DynastyChandelier   DynastyCandelabra                               SteampunkCup                     GypsyRobe                 SittingDucksFishingRod                                 TrapsightPotion         FishingSeaweed                                                                                                                         TheFishofCthulu                               ShipInABottle  PressureTrack                                                                                               TartarSauce                        Flairon                    SkywareWorkbench                                               AmberGemsparkWallOff  AmethystGemsparkWallOff  DiamondGemsparkWallOff  EmeraldGemsparkWallOff  RubyGemsparkWallOff  SapphireGemsparkWallOff  TopazGemsparkWallOff       ConfettiBlockBlack ConfettiWallBlack    AlphabetStatue0 AlphabetStatue1 AlphabetStatue2 AlphabetStatue3 AlphabetStatue4 AlphabetStatue5 AlphabetStatue6 AlphabetStatue7 AlphabetStatue8 AlphabetStatue9 AlphabetStatueA AlphabetStatueB AlphabetStatueC AlphabetStatueD AlphabetStatueE AlphabetStatueF AlphabetStatueG AlphabetStatueH AlphabetStatueI AlphabetStatueJ AlphabetStatueK AlphabetStatueL AlphabetStatueM AlphabetStatueN AlphabetStatueO AlphabetStatueP AlphabetStatueQ AlphabetStatueR AlphabetStatueS AlphabetStatueT AlphabetStatueU AlphabetStatueV AlphabetStatueW AlphabetStatueX AlphabetStatueY AlphabetStatueZ     MusicBoxUndergroundCrimson                        LunarTabletFragment      None None  None  None None  None  None None  None              LaserRuler AntiGravityHook                                                        WhiteLunaticHood BlueLunaticHood WhiteLunaticRobe BlueLunaticRobe     MartianArmorDye PaintingCastleMarsberg PaintingMartiaLisa PaintingTheTruthIsUpThere        BrownAndBlackDye  BrownAndSilverDye    None       BeesKnees              BlueCultistCasterBanner            DiablolistBanner                               MartianBrainscramblerBanner    MartianGreyGruntBanner  MartianRaygunnerBanner                                            SparkyPainting           SoulDrain                  DevDye       BottomlessBucket          MushroomDye   MusicBoxLunarBoss        ShadowFlameBow ShadowFlameHexDoll ShadowFlameKnife PaintingAcorns PaintingColdSnap PaintingCursedSaint PaintingSnowfellas PaintingTheSeason     Sundial  MarbleBlock  CordageGuide    GoldButterflyCage         Marble  MarbleBlockWall  LockBox Granite GraniteBlock  GraniteBlockWall  NightKey LightKey     EoCShield                       FishermansGuide  REK                                                                               TsunamiInABottle  CorruptFishingCrate CrimsonFishingCrate DungeonFishingCrate FloatingIslandFishingCrate HallowedFishingCrate JungleFishingCrate       DayBloomPlanterBox  CorruptPlanterBox CrimsonPlanterBox    FireBlossomPlanterBox BrainOfConfusion   BejeweledValkyrieHead BejeweledValkyrieBody BejeweledValkyrieWing RichGravestone1 RichGravestone2 RichGravestone3 RichGravestone4 RichGravestone5  MusicBoxMartians MusicBoxPirates MusicBoxHell  Trapdoor   TaxCollectorHat TaxCollectorSuit TaxCollectorPants  ClothierJacket ClothierPants DyeTraderTurban  BalloonHorseshoeFart BalloonHorseshoeHoney BalloonHorseshoeSharkron  CageEnchantedNightcrawler CageBuggy CageGrubby CageSluggy       BuccaneerShirt BuccaneerPants ObsidianHelm ObsidianShirt    Sandstone HardenedSand  CorruptHardenedSand CrimsonHardenedSand CorruptSandstone CrimsonSandstone WoodYoyo CorruptYoyo CrimsonYoyo JungleYoyo      RedsYoyo   HelFire  TheEyeOfCthulhu                       FormatC   KingSlimeBossBag EyeOfCthulhuBossBag EaterOfWorldsBossBag BrainOfCthulhuBossBag QueenBeeBossBag SkeletronBossBag WallOfFleshBossBag DestroyerBossBag TwinsBossBag SkeletronPrimeBossBag PlanteraBossBag GolemBossBag FishronBossBag CultistBossBag MoonLordBossBag HiveBackpack YoYoGlove    HallowHardenedSand HallowSandstone  CorruptHardenedSandWall CrimsonHardenedSandWall HallowHardenedSandWall CorruptSandstoneWall CrimsonSandstoneWall HallowSandstoneWall   DyeTradersScimitar PainterPaintballGun TaxCollectorsStickOfDoom StylistKilLaKillScissorsIWish MinecartMech    AncientCultistTrophy    LivingMahoganyLeafWand         MusicBoxTowers MusicBoxGoblins BossMaskCultist BossMaskMoonlord FossilHelm FossilShirt FossilPants   BoneDagger FossilOre  StardustBreastplate   StrangePlant1 StrangePlant2 StrangePlant3 StrangePlant4  GoblinSummonerBanner      DrManFlyBanner          GreekSkeletonBanner GraniteFlyerBanner      FlyingAntlionBanner WalkingAntlionBanner DesertGhoulBanner DesertLamiaBanner DesertDjinnBanner DesertBasiliskBanner RavagerScorpionBanner StardustSoldierBanner StardustWormBanner StardustJellyfishBanner StardustSpiderBanner StardustSmallCellBanner StardustLargeCellBanner SolarCoriteBanner SolarSrollerBanner SolarCrawltipedeBanner SolarDrakomireRiderBanner SolarDrakomireBanner SolarSolenianBanner NebulaSoldierBanner NebulaHeadcrabBanner NebulaBrainBanner NebulaBeastBanner VortexLarvaBanner VortexHornetQueenBanner VortexHornetBanner VortexSoldierBanner VortexRiflemanBanner             NebulaPickup1 NebulaPickup2 NebulaPickup3 FragmentVortex FragmentNebula FragmentSolar FragmentStardust LunarOre LunarBrick None None  None  LunarBar WingsSolar WingsVortex WingsNebula WingsStardust LunarBrickWall      TheBrideHat TheBrideDress                                           LunarHamaxeSolar LunarHamaxeVortex LunarHamaxeNebula LunarHamaxeStardust          ShiftingPearlSandsDye        DayBreak   FireworksLauncher  PartyGirlGrenade LunarCraftingStation FlameAndSilverDye GreenFlameAndSilverDye BlueFlameAndSilverDye     BlackAndWhiteDye  SilverAndBlackDye    SquirrelRed SquirrelGold SquirrelOrangeCage SquirrelGoldCage MoonlordBullet MoonlordArrow MoonlordTurretStaff LunarFlareBook   LunarBlockSolar LunarBlockVortex LunarBlockNebula LunarBlockStardust  Yoraiz0rShirt Yoraiz0rPants Yoraiz0rWings Yoraiz0rDarkness  Yoraiz0rHead  SkiphsHelm SkiphsShirt SkiphsPants SkiphsWings LokisHelm LokisShirt LokisPants     MoonLordPainting      LogicGateLamp_Off LogicGate_AND LogicGate_OR LogicGate_NAND LogicGate_NOR LogicGate_XOR LogicGate_NXOR ConveyorBeltLeft ConveyorBeltRight WireKite  LogicSensor_Sun LogicSensor_Moon LogicSensor_Above WirePipe  LogicGateLamp_On   TeamBlockRed TeamBlockRedPlatform  ActuationAccessory  WeightedPressurePlatePink    WeightedPressurePlateOrange WeightedPressurePlatePurple WeightedPressurePlateCyan TeamBlockGreen TeamBlockBlue TeamBlockYellow TeamBlockPink TeamBlockWhite TeamBlockGreenPlatform TeamBlockBluePlatform TeamBlockYellowPlatform TeamBlockPinkPlatform TeamBlockWhitePlatform  GemLockRuby GemLockSapphire GemLockEmerald GemLockTopaz GemLockAmethyst GemLockDiamond GemLockAmber             LogicGateLamp_Faulty  Fake_Chest Fake_GoldChest Fake_ShadowChest Fake_EbonwoodChest Fake_RichMahoganyChest Fake_PearlwoodChest Fake_IvyChest Fake_IceChest Fake_LivingWoodChest Fake_SkywareChest Fake_ShadewoodChest Fake_WebCoveredChest Fake_LihzahrdChest Fake_WaterChest Fake_JungleChest Fake_CorruptionChest Fake_CrimsonChest Fake_HallowedChest Fake_FrozenChest Fake_DynastyChest Fake_HoneyChest Fake_SteampunkChest Fake_PalmWoodChest Fake_MushroomChest Fake_BorealWoodChest Fake_SlimeChest Fake_GreenDungeonChest Fake_PinkDungeonChest Fake_BlueDungeonChest Fake_BoneChest Fake_CactusChest Fake_FleshChest Fake_ObsidianChest Fake_PumpkinChest Fake_SpookyChest Fake_GlassChest Fake_MartianChest Fake_MeteoriteChest Fake_GraniteChest Fake_MarbleChest Fake_newchest1 Fake_newchest2 ProjectilePressurePad            ZombieArmStatue   GeyserTrap UltraBrightCampfire   LogicSensor_Water LogicSensor_Lava LogicSensor_Honey LogicSensor_Liquid PartyBundleOfBalloonsAccessory PartyBalloonAnimal  FlowerBoyHat FlowerBoyShirt FlowerBoyPants SillyBalloonPink SillyBalloonPurple SillyBalloonGreen SillyStreamerBlue SillyStreamerGreen SillyStreamerPink  SillyBalloonTiedPink SillyBalloonTiedPurple SillyBalloonTiedGreen  PartyMonolith PartyBundleOfBalloonTile  SliceOfCake  SandFallWall SnowFallWall SandFallBlock SnowFallBlock SnowCloudBlock PedguinHat PedguinShirt PedguinPants SillyBalloonPinkWall SillyBalloonPurpleWall SillyBalloonGreenWall AviatorSunglasses         AntlionClaw AncientArmorHat AncientArmorShirt AncientArmorPants AncientBattleArmorHat AncientBattleArmorShirt AncientBattleArmorPants     AncientBattleArmorMaterial LamiaPants LamiaShirt LamiaHat   SandsharkBanner SandsharkCorruptBanner SandsharkCrimsonBanner SandsharkHallowedBanner TumbleweedBanner  DjinnLamp MusicBoxSandstorm ApprenticeHat ApprenticeRobe ApprenticeTrousers SquireGreatHelm SquirePlating SquireGreaves HuntressWig HuntressJerkin HuntressPants MonkBrows MonkShirt MonkPants ApprenticeScarf SquireShield HuntressBuckler MonkBelt    DD2ElderCrystalStand  DD2FlameburstTowerT1Popper DD2FlameburstTowerT2Popper DD2FlameburstTowerT3Popper AleThrowingGlove DD2EnergyCrystal DD2SquireDemonSword DD2BallistraTowerT1Popper DD2BallistraTowerT2Popper DD2BallistraTowerT3Popper DD2SquireBetsySword DD2ElderCrystal DD2LightningAuraT1Popper DD2LightningAuraT2Popper DD2LightningAuraT3Popper DD2ExplosiveTrapT1Popper DD2ExplosiveTrapT2Popper DD2ExplosiveTrapT3Popper MonkStaffT1 MonkStaffT2 DD2GoblinBomberBanner DD2GoblinBanner DD2SkeletonBanner DD2DrakinBanner DD2KoboldFlyerBanner DD2KoboldBanner DD2WitherBeastBanner DD2WyvernBanner DD2JavelinThrowerBanner DD2LightningBugBanner None None None None None BookStaff BoringBow DD2PhoenixBow DD2PetGato DD2PetGhost DD2PetDragon MonkStaffT3 DD2BetsyBow BossBagBetsy None None BossMaskBetsy BossMaskDarkMage BossMaskOgre BossTrophyBetsy BossTrophyDarkmage BossTrophyOgre MusicBoxDD2 ApprenticeStaffT3 SquireAltHead SquireAltShirt SquireAltPants ApprenticeAltHead ApprenticeAltShirt ApprenticeAltPants HuntressAltHead HuntressAltShirt HuntressAltPants MonkAltHead MonkAltShirt MonkAltPants BetsyWings   Fake_CrystalChest Fake_GoldenChest            SkywareClock2 DungeonClockBlue DungeonClockGreen DungeonClockPink   DynastyPlatform    CrystalWorkbench GoldenWorkbench       CrystalBookCase CrystalSofaHowDoesThatEvenWork   ArkhalisHat ArkhalisShirt ArkhalisPants ArkhalisWings LeinforsHat LeinforsShirt LeinforsPants LeinforsWings LeinforsAccessory Celeb2                SpiderSinkSpiderSinkDoesWhateverASpiderSinkDoes   SpiderWorkbench Fake_SpiderChest                         LesionWorkbench Fake_LesionChest  None WoodenCrateHard IronCrateHard GoldenCrateHard CorruptFishingCrateHard CrimsonFishingCrateHard DungeonFishingCrateHard FloatingIslandFishingCrateHard HallowedFishingCrateHard JungleFishingCrateHard     BerserkerGlove       LavaSkull           None                             GolfClubIron  FlowerPacketBlue FlowerPacketMagenta FlowerPacketPink FlowerPacketRed FlowerPacketYellow FlowerPacketViolet FlowerPacketWhite FlowerPacketTallGrass       SandBoots  CarbonGuitar None  SuperStarCannon ThunderSpear ThunderStaff   PicnicTableWithCloth  FishMinecart FairyCritterPink FairyCritterGreen FairyCritterBlue       MusicBoxOceanAlt MusicBoxSlimeRain MusicBoxSpaceAlt MusicBoxTownDay MusicBoxTownNight MusicBoxWindyDay GolfCupFlagWhite GolfCupFlagRed GolfCupFlagGreen GolfCupFlagBlue GolfCupFlagYellow GolfCupFlagPurple  ShellPileBlock AntiPortalBlock GolfClubPutter GolfClubWedge GolfClubDriver  ToiletEbonyWood ToiletRichMahogany ToiletPearlwood ToiletLivingWood ToiletCactus ToiletBone ToiletFlesh ToiletMushroom ToiletSunplate ToiletShadewood ToiletLihzhard ToiletDungeonBlue ToiletDungeonGreen ToiletDungeonPink ToiletObsidian ToiletFrozen ToiletGlass ToiletHoney ToiletSteampunk ToiletPumpkin ToiletSpooky ToiletDynasty ToiletPalm ToiletBoreal ToiletSlime ToiletMartian ToiletGranite ToiletMarble ToiletCrystal ToiletSpider ToiletLesion ToiletDiamond MaidHead MaidShirt MaidPants VoidLens MaidHead2 MaidShirt2 MaidPants2 GolfHat GolfShirt GolfPants GolfVisor SpiderBlock SpiderWall ToiletMeteor LesionStation                     SolarWorkbench Fake_SolarChest                    VortexWorkbench Fake_VortexChest                    NebulaWorkbench Fake_NebulaChest                    StardustWorkbench Fake_StardustChest          MusicBoxDayRemix    FlowerPacketWild GolfBallDyedBlack GolfBallDyedBlue GolfBallDyedBrown GolfBallDyedCyan GolfBallDyedGreen GolfBallDyedLimeGreen GolfBallDyedOrange GolfBallDyedPink GolfBallDyedPurple GolfBallDyedRed GolfBallDyedSkyBlue GolfBallDyedTeal GolfBallDyedViolet GolfBallDyedYellow       MysticCoilSnake  GolfCart  Fake_GolfChest DesertChest Fake_DesertChest  SharpTears BloodMoonStarter DripplerFlail   GoldGoldfishBowl CatBast GoldStarryGlassBlock BlueStarryGlassBlock GoldStarryGlassWall BlueStarryGlassWall BabyBirdStaff   BlackCurrant    Dragonfruit         Starfruit                  SandstoneWorkbench  BloodHamaxe    GameMasterShirt GameMasterPants   BloodFishingRod FoodPlatter               PortableStool  PaperAirplaneA PaperAirplaneB   ZapinatorGray ZapinatorOrange        MusicBoxTitleAlt MusicBoxStorm MusicBoxGraveyard   LadyBug GoldLadyBug    EucaluptusSap KiteBlue KiteBlueAndYellow KiteRed KiteRedAndYellow KiteYellow IvyGuitar       KiteWyvern   CombatBook                      FloatingTube  FrozenCrateHard  OasisCrateHard         OasisFountain    MusicBoxUndergroundJungle                      HellMinecart WitchBroom                  TurtleJungleCage   TurtleJungle     PartyMinecart PirateMinecart      LuckPotionLesser  LuckPotionGreater     TimerOneHalfSecond TimerOneFourthSecond EbonstoneEcho MudWallEcho PearlstoneEcho SnowWallEcho AmethystEcho TopazEcho SapphireEcho EmeraldEcho RubyEcho DiamondEcho Cave1Echo Cave2Echo Cave3Echo Cave4Echo Cave5Echo Cave6Echo Cave7Echo SpiderEcho CorruptGrassEcho HallowedGrassEcho IceEcho ObsidianBackEcho CrimsonGrassEcho CrimstoneEcho CaveWall1Echo CaveWall2Echo Cave8Echo Corruption1Echo Corruption2Echo Corruption3Echo Corruption4Echo Crimson1Echo Crimson2Echo Crimson3Echo Crimson4Echo Dirt1Echo Dirt2Echo Dirt3Echo Dirt4Echo Hallow1Echo Hallow2Echo Hallow3Echo Hallow4Echo Jungle1Echo Jungle2Echo Jungle3Echo Jungle4Echo Lava1Echo Lava2Echo Lava3Echo Lava4Echo Rocks1Echo Rocks2Echo Rocks3Echo Rocks4Echo   EyeballFlyingFishBanner   GoblinSharkBanner LargeBambooBlock LargeBambooBlockWall   HellCake     ChefShirt       UnicornHornHat BambooBlock BambooBlockWall                   BambooWorkbench Fake_BambooChest  GolfClubStoneIron GolfClubRustyPutter GolfClubBronzeWedge GolfClubWoodDriver GolfClubMythrilIron GolfClubLeadPutter GolfClubGoldWedge GolfClubPearlwoodDriver GolfClubTitaniumIron GolfClubShroomitePutter GolfClubDiamondWedge GolfClubChlorophyteDriver GolfTrophyBronze GolfTrophySilver GolfTrophyGold BloodNautilusBanner  ExoticEasternChewToy  MusicBoxJungleNight StormTigerStaff   KiteBoneSerpent KiteWorldFeeder KiteBunny KitePigron    BananaDaiquiri  PinaColada          JawsOfDeath TheSandsOfSlime SnakesIHateSnakes LifeAboveTheSand     VisitingThePyramids          AmberStoneWallEcho KiteManEater KiteJellyfishBlue KiteJellyfishPink KiteShark SuperHeroMask SuperHeroCostume SuperHeroTights    GolfPainting1 GolfPainting2 GolfPainting3 GolfPainting4   PrettyPinkDressSkirt PrettyPinkDressPants   GlowPaint KiteSandShark KiteBunnyCorrupt KiteBunnyCrimson BlandWhip DrumStick KiteGoldfish KiteAngryTrapper KiteKoi KiteCrawltipede SwordWhip MaceWhip ScytheWhip KiteSpectrum  KiteWanderingEye KiteUnicorn UndertakerHat UndertakerCoat DandelionBanner        SoulBottleLight SoulBottleNight SoulBottleFlight SoulBottleSight SoulBottleMight SoulBottleFright   QuadBarrelShotgun        GravediggerShovel DungeonDesertChest Fake_DungeonDesertChest DungeonDesertKey SparkleGuitar       None        GhostarsWings  GhostarSkullPin GhostarShirt GhostarPants BallOfFuseWire   DrManFlyMask DrManFlyLabCoat  ButcherApron ButcherPants    SafemanWings SafemanSunHair SafemanSunDress SafemanDressLeggings FoodBarbarianWings FoodBarbarianHelm FoodBarbarianArmor FoodBarbarianGreaves GroxTheGreatWings GroxTheGreatHelm GroxTheGreatArmor GroxTheGreatGreaves Smolstar  BouncingShield   DiggingMoleMinecart    DontHurtCrittersBook           HallowBossDye    FairyQueenBossBag FairyQueenTrophy FairyQueenMask PaintedHorseSaddle MajesticHorseSaddle DarkHorseSaddle   HallowJoustingLance  PirateShipMountItem SpookyWoodMountItem SantankMountItem WallOfFleshGoatMountItem DarkMageBookMountItem KingSlimePetItem EyeOfCthulhuPetItem EaterOfWorldsPetItem BrainOfCthulhuPetItem SkeletronPetItem QueenBeePetItem DestroyerPetItem TwinsPetItem SkeletronPrimePetItem PlanteraPetItem GolemPetItem DukeFishronPetItem LunaticCultistPetItem MoonLordPetItem FairyQueenPetItem PumpkingPetItem EverscreamPetItem IceQueenPetItem MartianPetItem DD2OgrePetItem DD2BetsyPetItem    FireproofBugNet  RainbowWings      LicenseCat LicenseDog GemSquirrelAmethyst GemSquirrelTopaz GemSquirrelSapphire GemSquirrelEmerald GemSquirrelRuby GemSquirrelDiamond GemSquirrelAmber GemBunnyAmethyst GemBunnyTopaz GemBunnySapphire GemBunnyEmerald GemBunnyRuby GemBunnyDiamond GemBunnyAmber       GemTreeTopazSeed GemTreeAmethystSeed GemTreeSapphireSeed GemTreeEmeraldSeed GemTreeRubySeed GemTreeDiamondSeed GemTreeAmberSeed PotSuspended PotSuspendedDaybloom PotSuspendedMoonglow PotSuspendedWaterleaf PotSuspendedShiverthorn PotSuspendedBlinkroot PotSuspendedDeathweedCorrupt PotSuspendedDeathweedCrimson PotSuspendedFireblossom BrazierSuspended VolcanoSmall VolcanoLarge PotionOfReturn VanityTreeSakuraSeed    TeleportationPylonJungle TeleportationPylonPurity LavaCrate LavaCrateHard ObsidianLockbox LavaFishbowl LavaFishingHook                     PottedLavaPlantPalm PottedLavaPlantBush PottedLavaPlantBramble PottedLavaPlantBulb PottedLavaPlantTendrils VanityTreeYellowWillowSeed  DirtStickyBomb LicenseBunny  FireWhip ThornWhip RainbowWhip  TeleportationPylonHallow TeleportationPylonUnderground TeleportationPylonOcean TeleportationPylonDesert TeleportationPylonSnow TeleportationPylonMushroom CavernFountain PiercingStarlight EyeofCthulhuMasterTrophy EaterofWorldsMasterTrophy BrainofCthulhuMasterTrophy SkeletronMasterTrophy QueenBeeMasterTrophy KingSlimeMasterTrophy WallofFleshMasterTrophy TwinsMasterTrophy DestroyerMasterTrophy SkeletronPrimeMasterTrophy PlanteraMasterTrophy GolemMasterTrophy DukeFishronMasterTrophy LunaticCultistMasterTrophy MoonLordMasterTrophy UFOMasterTrophy FlyingDutchmanMasterTrophy MourningWoodMasterTrophy PumpkingMasterTrophy IceQueenMasterTrophy EverscreamMasterTrophy SantankMasterTrophy DarkMageMasterTrophy OgreMasterTrophy BetsyMasterTrophy FairyQueenMasterTrophy QueenSlimeMasterTrophy TeleportationPylonVictory FairyQueenMagicItem FairyQueenRangedItem LongRainbowTrailWings RabbitOrder  QueenSlimeBossBag   QueenSlimePetItem EmpressButterfly AccentSlab  EmpressButterflyJar     LarvaeAntlionBanner CrimsonBunnyBanner CrimsonGoldfishBanner CrimsonPenguinBanner BigMimicCorruptionBanner BigMimicCrimsonBanner BigMimicHallowBanner   CreativeWings MusicBoxQueenSlime QueenSlimeHook QueenSlimeMountSaddle CrystalNinjaHelmet CrystalNinjaChestplate CrystalNinjaLeggings MusicBoxEmpressOfLight GelBalloon  QueenSlimeCrystal EmpressFlightBooster MusicBoxDukeFishron MusicBoxMorningRain MusicBoxConsoleTitle  GraduationCapBlue GraduationCapMaroon GraduationCapBlack GraduationGownBlue GraduationGownMaroon GraduationGownBlack    OceanCrateHard  EmpressBlade MusicBoxUndergroundDesert  TeaKettle     SleepingIcon MusicBoxOWRain MusicBoxOWDay MusicBoxOWNight MusicBoxOWUnderground MusicBoxOWDesert MusicBoxOWOcean MusicBoxOWMushroom MusicBoxOWDungeon MusicBoxOWSpace MusicBoxOWUnderworld MusicBoxOWSnow MusicBoxOWCorruption MusicBoxOWUndergroundCorruption MusicBoxOWCrimson MusicBoxOWUndergroundCrimson MusicBoxOWUndergroundSnow MusicBoxOWUndergroundHallow MusicBoxOWBloodMoon MusicBoxOWBoss2 MusicBoxOWBoss1 MusicBoxOWInvasion MusicBoxOWTowers MusicBoxOWMoonLord MusicBoxOWPlantera MusicBoxOWJungle MusicBoxOWWallOfFlesh MusicBoxOWHallow MilkCarton CoffeeCup  MusicBoxCredits PlaguebringerHelmet PlaguebringerChestplate PlaguebringerGreaves RoninHat RoninShirt RoninPants TimelessTravelerHood TimelessTravelerRobe TimelessTravelerBottom  FloretProtectorChestplate FloretProtectorLegs CapricornMask  CapricornLegs  TVHeadMask TVHeadSuit TVHeadPants  PrincessWeapon       RoyalDressTop RoyalDressBottom BoneWhip       PottedCrystalPlantFern PottedCrystalPlantSpiral PottedCrystalPlantTeardrop PottedCrystalPlantTree  PaintingOfALass DarkSideHallow BerniePetItem GlommerPetItem DeerclopsPetItem PigPetItem    LucyTheAxe   ChesterPetItem GarlandHat   WilsonShirt WilsonPants WilsonBeardShort WilsonBeardLong WilsonBeardMagnificent    DeerclopsMasterTrophy DeerclopsBossBag MusicBoxDeerclops DontStarveShaderItem  WillowShirt WillowSkirt PewMaticHorn    PaintingWilson PaintingWillow PaintingWendy PaintingWolfgang FartMinecart  VioletMoss RainbowMoss  WolfMountItem    Clentaminator2  VulkelfEar StinkbugHousingBlocker StinkbugHousingBlockerEcho  FishingBobberGlowingStar FishingBobberGlowingLava FishingBobberGlowingKrypton FishingBobberGlowingXenon FishingBobberGlowingArgon FishingBobberGlowingViolet FishingBobberGlowingRainbow  CoralBathtub CoralBed CoralBookcase CoralDresser CoralCandelabra CoralCandle CoralChair CoralChandelier CoralChest CoralClock CoralDoor CoralLamp CoralLantern CoralPiano CoralPlatform CoralSink CoralSofa CoralTable CoralWorkbench Fake_CoralChest CoralToilet                   BalloonWorkbench Fake_BalloonChest                    AshWoodWorkbench Fake_AshWoodChest                                DoNotEattheVileMushroom YuumaTheBlueTiger MoonmanandCompany           BlessingfromTheHeavens                      JunimoPetItem            TerraFartMinecart MinecartPowerup     HiveFive AcornAxe  BlueEgg        GoblorcEar   PlacePainting DontHurtNatureBook           PlaceableHealingPotion PlaceableManaPotion  DontHurtComboBook RubblemakerSmall     RubblemakerMedium RubblemakerLarge HorseshoeBundle   MechdusaSummon RodOfHarmony CombatBookVolumeTwo AegisCrystal        EchoMonolith  ShimmerMonolith  ShimmerBlock Shimmerfly ShimmerflyinaBottle  ShimmerTorch  ShimmerCloak  ShimmerCampfire Shellphone ShellphoneSpawn ShellphoneOcean ShellphoneHell MusicBoxShimmer SpiderWallUnsafe  BlueBrickWallUnsafe BlueSlabWallUnsafe BlueTiledWallUnsafe PinkBrickWallUnsafe PinkSlabWallUnsafe PinkTiledWallUnsafe GreenBrickWallUnsafe GreenSlabWallUnsafe GreenTiledWallUnsafe SandstoneWallUnsafe HardenedSandWallUnsafe LihzahrdWallUnsafe     Moondial WaffleIron   DizzyHat LincolnsHoodie LincolnsPants SunOrnament HoplitePizza LincolnsHood  SandSolution SnowSolution DirtSolution PoopBlock PoopWall ShimmerWall ShimmerBrick ShimmerBrickWall DirtiestBlock                 AncientBlueDungeonBrick AncientBlueDungeonBrickWall AncientGreenDungeonBrick AncientGreenDungeonBrickWall AncientPinkDungeonBrick AncientPinkDungeonBrickWall               ShellphoneDummy  LavaMossBlock ArgonMossBlock KryptonMossBlock XenonMossBlock VioletMossBlock RainbowMossBlock LavaMossBlockWall ArgonMossBlockWall KryptonMossBlockWall XenonMossBlockWall VioletMossBlockWall RainbowMossBlockWall JimsDrone JimsDroneVisor DontHurtCrittersBookInactive DontHurtNatureBookInactive DontHurtComboBookInactive DeadCellsMushroomBoiSummonItem DeadCellsBeheadedHead DeadCellsBeheadedBody DeadCellsBeheadedLegs DeadCellsBarrelLauncher DeadCellsKillingDeck DeadCellsFlint DeadCellsBarnacleSummonItem MiteyTitey DeadCellsRamRune DeadCellsSwarmGrenade DemonAltarReplica CrimsonAltarReplica ShadowOrbReplica CrimsonHeartReplica CobwebReplica DeadCellsDisplayJar CobWhip CorruptWhip CrimsonWhip MeteorWhip FlowerWhip EelWhip ConstellationWhip MoonLordWhip  DeadCellsPotionStation QueenOfBees PlayerVoiceCowbellItem PlayerVoiceChickenFeetItem TheSeaOfSilence       ShimmerFallBlock ShimmerFallWall  LifeFruitHealingPotion   PlayerVoiceFrogItem PlayerVoiceGoatItem PlayerVoiceRetroItem PlayerVoiceCatItem PlayerVoiceDogItem PlayerVoiceTurkeyItem PlayerVoiceGoblinItem PlayerVoiceCrowItem PlayerVoiceBalloonItem PlayerVoiceUndeadItem PlayerVoiceVampireItem VelociraptorMountItem   PufferfishPet  MoonLordBody Poulder AxeFairyPetItem SuperFertilizer     BoulderPet  RatMountItem FlaironFlail OfSeaAndDreams  BannerOfTheBeast StickmanVsTerrTerr    PlayerVoiceFairyItem    MusicBoxQueenBee MusicBoxTwins      LivingWoodWallUnsafe DirtWallUnsafe                    AetheriumWorkbench Fake_AetheriumChest     MudBallPlayer TorchGodPotion     MusicBoxKingSlime MusicBoxQueenBeeAlt MusicBoxLunaticCultist MusicBoxSkeletronPrime MusicBoxDestroyer ChickenBonesHead ChickenBonesBody ChickenBonesLegs   UpgradedMiningHead UpgradedMiningBody UpgradedMiningLegs UpgradedFishingHead UpgradedFishingBody UpgradedFishingLegs SuperBomb SuperStickyBomb  BatMountItem   RollerSkatesBlueMountItem                   FallenStarWorkbench Fake_FallenStarChest    ChippysHead ChippysBody ChippysLegs ChippysWings   PaintingRPlace2023 PaintingBouldChoices PaintingDarkForebodings PaintingGermanZenith PaintingItsScragglinTime PaintingKaguya PaintingGermanBeer MusicBoxEaterOfWorlds MusicBoxTorchGod MusicBoxTorchGodAlt RollerSkatesGreenMountItem RollerSkatesClassicMountItem RollerSkatesPartyMountItem    BlueBikiniBody BlueBikiniLegs     TeleportationPylonUnderworld TeleportationPylonShimmer RainbowBoulderPet NoirMonolith HeroicisHead HeroicisBody HeroicisLegs   HeroicisWingsInactive  PalworldMinionCattiva PalworldMinionFoxsparks PalworldPetChillet PalworldPetChilletIgnis PalworldDigtoise SoundGun      TeamBlockRedVariant TeamBlockGreenVariant TeamBlockBlueVariant TeamBlockYellowVariant TeamBlockPinkVariant TeamBlockWhiteVariant LilacDuskHead LilacDuskBody LilacDuskLegs                         FeywoodWorkbench Fake_FeywoodChest            HallowedFurnitureChest          HallowedWorkbench Fake_HallowedFurnitureChest    PalworldPalMetalArmorBody PalworldPalMetalArmorLegs ChippysWingsInactive RemoteControlCar                Fake_GothicChest                     DemoniteWorkbench Fake_DemoniteChest                    CrimtaneWorkbench Fake_CrimtaneChest                    SnowWorkbench Fake_SnowChest                    FlinxFurWorkbench Fake_FlinxFurChest                 PineWorkbench Fake_PineChest                    EasterWorkbench Fake_EasterChest                  StoneWorkbench Fake_StoneChest                    JellyfishWorkbench Fake_JellyfishChest PineTreeBlockWall  EasterBlockWall    FlinxFurBlockWall  JellyfishBlockWall ToyBreakerBlock PaintingRemix PineWoodBlock PineWoodBlockWall                    HarpyWorkbench Fake_HarpyChest  HarpyBlockWall                   CloudWorkbench Fake_CloudChest MoonplateBathtub MoonplateBed MoonplateBookcase MoonplateCandelabra MoonplateCandle MoonplateChair MoonplateChandelier MoonplateChest MoonplateClock MoonplateDoor MoonplateDresser MoonplateLamp MoonplateLantern MoonplatePiano MoonplatePlatform MoonplateSink MoonplateSofa MoonplateTable MoonplateToilet MoonplateWorkbench Fake_MoonplateChest  MoonplateBlockWall                    LibrarianWorkbench Fake_LibrarianChest  LibrarianBlockWall                    SpikeWorkbench Fake_SpikeChest  SpikeBlockWall                    OfficeWorkbench Fake_OfficeChest  OfficeBlockWall                    ForbiddenWorkbench Fake_ForbiddenChest  ForbiddenBlockWall WaterBathtub WaterBed WaterBookcase WaterCandelabra WaterFurnitureCandle WaterChair WaterChandelier WaterClock WaterDoor WaterDresser WaterLamp WaterLantern WaterPiano WaterPlatform WaterSink WaterSofa WaterTable WaterToilet WaterWorkbench WaterBlock WaterBlockWall                    BoulderWorkbench Fake_BoulderChest  BoulderBlockWall DamagingSpikeBlock DemonAltarIcon CrimsonAltarIcon LunasHead LunasBody LunasLegs LunasWings LunasCloak PalworldChilletEgg FoxparksTagEffect MusicBoxSkeletron None".split(" ");
    za.meta = '|s=9999;d|s=9999|d=5|t=20|k=2|tp=40|rg=1|z=2000;t|s=9999|rg=100;t|s=9999|rg=100;d|s=9999|d=12|t=20|k=5.5|rg=1|z=1800;c|s=9999|hl=15|t=17|rg=30|z=1250;d|s=9999|d=8|t=12|k=4|rg=1|z=1400;d|s=9999|d=7|t=30|k=5.5|th=40|rg=1|z=1600;t|s=9999|rg=100|1=Provides light|z=50;t|s=9999|rg=100;d|s=9999|d=5|t=27|k=4.5|tx=9|rg=1|z=1600;t|s=9999|rg=100|z=500;t|s=9999|rg=100|z=250;t|s=9999|rg=100|z=1500;t|s=9999|rg=100|z=750;a|s=9999|rg=1|1=Tells the time|z=1000;a|s=9999|rg=1|1=Tells the time|z=5000;a|s=9999|rg=1|1=Tells the time|z=10000;a|s=9999|rg=1|1=Displays depth|z=12500;t|s=9999|rg=25|z=6000;t|s=9999|rg=25|z=750;t|s=9999|rg=25|z=3000;t|s=9999|rg=25|z=1500;b|s=9999|rg=99|1=Both tasty and flammable|z=5;d|s=9999|d=7|t=20|k=5|rg=1|z=100;t|s=9999|rg=1|z=200;w|s=9999|rg=400;b|s=9999|rg=50|z=10;c|s=9999|hl=50|t=17|rg=30|z=300;c|s=9999|r=2|t=30|rg=10|1=Permanently increases maximum life by 20|z=75000;w|s=9999|rg=400;t|s=9999|rg=25|z=20;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1=Used for smelting ore|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|1=Used to craft items from metal bars|z=5000;t|s=9999|rg=1|1=Used for basic crafting|z=150;4|s=9999|D=1|rg=1|z=1000;|s=9999|rg=25|z=500;r|s=9999|d=4|t=30|rg=1|z=100;rb|s=9999|d=5|k=2|rg=99|z=5;rb|s=9999|d=7|k=2|rg=99|z=10;rc|s=9999|d=10|t=15|rg=99|z=15;c|s=9999|t=45|rg=3|1=Summons the Eye of Cthulhu;r|s=9999|d=14|t=25|k=1|rg=1|z=18000;d|s=9999|d=20|t=30|k=6|tx=15|rg=1|z=13500;d|s=9999|d=16|t=20|k=5|rg=1|z=13500;rb|s=9999|d=12|k=3|rg=99|z=40;t|s=9999|rg=1|z=500;a|s=9999|rg=1|1=Slowly regenerates life|z=50000;|s=9999|t=90|rg=1|1=Gaze in the mirror to return home|z=50000;rb|s=9999|d=10|k=4|rg=99|z=100;t|s=9999|rg=1|z=300;a|s=9999|rg=1|1=Allows the holder to double jump|z=50000;a|s=9999|rg=1|1=The wearer can run super fast|z=50000;d|s=9999|d=17|t=20|k=8|rg=1|z=50000;t|s=9999|rg=100|1=Pulsing with dark energy|z=5000;t|s=9999|rg=25|1=Pulsing with dark energy|z=15000;1;t|s=9999|rg=25|z=500;|s=9999|rg=25|z=50;t|s=9999|rg=100;t|s=9999|rg=25|z=20;t|s=9999|rg=5|z=5000;m|s=9999|d=10|t=28|k=1|m=10|rg=1|1=Summons a vile thorn|z=75000;d|s=9999|r=2|d=25|t=20|k=5|rg=1|1=Causes stars to rain from the sky|2=Forged with the fury of heaven|z=50000;c|s=9999|t=15|rg=99|1=Cleanses the evil|z=75;c|s=9999|t=15|rg=99|1=Spreads the Corruption|z=100;|s=9999|rg=25|1=Looks tasty!|z=10;|s=9999|rg=25|z=100;c|s=9999|t=45|rg=3|1=Summons the Eater of Worlds;rb|s=100|d=25|rg=100|z=5;rb|s=100|d=50|rg=100|z=500;rb|s=100|d=100|rg=100|z=50000;rb|s=9999|d=200|rg=100|z=5000000;b|s=9999|rg=50|1=Disappears after the sunrise|z=2500;6|s=9999|D=1|rg=1|z=1000;6|s=9999|D=2|rg=1|z=4000;6|s=9999|D=3|rg=1|z=10000;6|s=9999|D=4|rg=1|z=20000;5|s=9999|D=2|rg=1|z=1250;5|s=9999|D=3|rg=1|z=5000;5|s=9999|D=4|rg=1|z=12500;5|s=9999|D=5|rg=1|z=25000;|s=9999|t=20|k=7|rg=1|1=Get over here!|2={$CommonItemTooltip.Hook}|z=20000;t|s=9999|tr=3|rg=100|1=Can be climbed on|z=200;c|s=9999|t=15|rg=25|z=500;t|s=9999|rg=1|1={$CommonItemTooltip.PersonalStorage}|z=10000;4|s=9999|D=2|rg=1|1=Provides light when worn|z=40000;4|s=9999|D=1|rg=1|z=750;4|s=9999|D=2|rg=1|z=3000;4|s=9999|D=3|rg=1|z=7500;4|s=9999|D=4|rg=1|z=15000;w|s=9999|rg=400;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};r|s=9999|c=6|d=13|t=16|k=1|rg=1|z=50000;r|s=9999|c=8|d=31|t=32|k=5.25|rg=1|1=I have to be ready, said the minuteman|z=75000;rb|s=9999|d=7|k=2|rg=99|z=7;r|s=9999|r=2|d=6|t=8|rg=1|1=33% chance to save ammo|2=Half shark, half gun, completely awesome.|z=350000;r|s=9999|d=8|t=28|rg=1|z=1400;6|s=9999|D=6|rg=1|1=5% increased critical strike chance|z=22500;5|s=9999|D=7|rg=1|1=5% increased critical strike chance|z=30000;4|s=9999|D=6|rg=1|1=5% increased critical strike chance|z=37500;d|s=9999|d=9|t=20|k=3|tp=65|rg=1|1=Able to mine Hellstone|z=18000;d|s=9999|d=24|t=45|k=6|th=55|rg=1|z=15000;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=12000;t|s=9999|rg=1|z=24000;c|s=9999|r=2|t=30|rg=10|1=Permanently increases maximum mana by 20|z=12500;c|s=9999|hm=50|t=17|rg=30|z=100;a|s=9999|rg=1|1=Increases maximum mana by 20|z=75000;m|s=9999|r=3|d=48|t=16|k=5.5|m=12|rg=1|1=Throws balls of fire|z=125000;m|s=9999|r=2|d=35|t=22|k=7.5|m=14|rg=1|1=Casts a controllable missile|z=87500;|s=9999|t=20|k=5|rg=1|1=Magically moves dirt|z=50000;|s=9999|t=20|rg=1|1=Creates a magical shadow orb|z=75000;t|s=9999|rg=100|1={$CommonItemTooltip.BurningBlock}|z=1000;t|s=9999|rg=25|1=Warm to the touch|z=7000;|s=9999|rg=1|1=Sometimes dropped by Skeletons and Piranha|z=1000;d|s=9999|r=3|d=49|t=20|k=8|rg=1|z=100000;r|s=9999|r=3|d=31|t=22|k=2|rg=1|1=Lights wooden arrows ablaze|z=27000;d|s=9999|r=3|d=40|t=40|k=6.5|rg=1|1=Its made out of fire!|z=27000;d|s=9999|r=3|d=12|t=23|k=2|tp=100|rg=1|z=27000;4|s=9999|D=5|rg=1|1=9% increased magic damage|z=45000;5|s=9999|D=6|rg=1|1=9% increased magic damage|z=30000;6|s=9999|D=5|rg=1|1=9% increased magic damage|z=30000;c|s=9999|hl=30|t=17|rg=30|z=20;m|s=9999|d=20|t=17|k=0.75|m=6|rg=1|z=20000;a|s=9999|r=3|rg=1|1=Allows flight|z=50000;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=150;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100|1={$CommonItemTooltip.ContactDamageBlock};t|s=9999|rg=1|1=Holding this may attract unwanted attention|z=500;t|s=9999|rg=25|1={$CommonItemTooltip.PlacementStyle}|z=1500;t|s=9999|rg=50;4|s=9999|r=2|D=6|rg=1|1=5% increased ranged damage|z=45000;5|s=9999|r=2|D=7|rg=1|1=5% increased ranged damage|z=30000;6|s=9999|r=2|D=6|rg=1|1=5% increased ranged damage|z=30000;rc|s=9999|d=20|t=12|k=2.3|rg=99|z=50;d|s=9999|r=2|d=24|t=18|k=3|rg=1|z=87500;a|s=9999|r=2|D=1|rg=1|1=Grants immunity to knockback|z=87500;m|s=9999|r=2|d=27|t=16|k=7|m=7|rg=1|1=Sprays out a shower of water|z=87500;a|s=9999|rg=1|1=Negates fall damage|2={$CommonItemTooltip.GoodFortune}|z=27000;a|s=9999|rg=1|1=Increases jump height|z=75000;r|s=9999|r=2|d=25|t=30|k=6|rg=1|z=27000;rc|s=9999|d=16|t=15|k=1|rg=99|z=80;d|s=9999|d=15|t=45|k=5.5|rg=1|z=75000;d|s=9999|r=2|d=27|t=45|k=6|rg=1|z=87500;r|s=9999|r=2|d=26|t=15|k=3|rg=1|z=87500;m|s=9999|r=2|d=19|t=17|k=5|m=10|rg=1|1=Casts a slow moving bolt of water|z=75000;c|s=9999|t=25|rg=99|1=A small explosion that will destroy most tiles|z=300;c|s=9999|t=40|rg=99|1=A large explosion that will destroy most tiles|z=2000;rc|s=9999|d=60|t=45|k=8|rg=99|1=A small explosion that will not destroy tiles|z=75;b|s=9999|rg=100|1={$CommonItemTooltip.SuffocateBlock};t|s=9999|rg=100;t|s=9999|rg=1;t|s=9999|rg=100;t|s=9999|rg=100|1={$CommonItemTooltip.CanBeExtractinated};t|s=9999|r=2|rg=100|1={$CommonItemTooltip.BurningBlock}|z=1250;t|s=9999|r=2|rg=25|1=Hot to the touch|z=20000;t|s=9999|rg=100;t|s=9999|rg=15|z=5625;t|s=9999|rg=15|z=11250;t|s=9999|rg=15|z=7500;t|s=9999|rg=15|z=3750;t|s=9999|rg=15|z=1875;t|s=9999|rg=15|z=15000;t|s=9999|rg=100|z=50;1;|s=9999|r=3|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=20000;d|s=9999|d=10|t=27|k=4|rg=1|1=Increases breath time and allows breathing in water|z=10000;a|s=9999|rg=1|1=Grants the ability to swim|z=10000;c|s=9999|hl=100|t=17|rg=30|z=1000;c|s=9999|hm=100|t=17|rg=30|z=250;d|s=9999|r=3|d=18|t=20|k=4.5|rg=1|1=Has a chance to poison enemies|z=27000;d|s=9999|r=3|d=25|t=15|k=8|rg=1|z=50000;t|s=9999|rg=100;a|s=9999|r=2|D=1|rg=1|1=Grants immunity to fire blocks|z=27000;t|s=9999|rg=25|z=150;t|s=9999|rg=25|z=150;d|s=9999|d=2|t=37|k=5.5|th=25|rg=1|z=50;r|s=9999|r=2|d=55|t=12|k=3|rg=1|1=Shoots fallen stars|z=500000;d|s=9999|d=26|t=18|k=3|rg=1|z=27000;d|s=9999|d=26|t=18|k=3|rg=1|z=27000;d|s=9999|d=26|t=18|k=3|rg=1|z=27000;d|s=9999|d=26|t=18|k=3|rg=1|z=27000;d|s=9999|d=26|t=18|k=3|rg=1|z=27000;d|s=9999|d=26|t=18|k=3|rg=1|z=27000;d|s=9999|d=20|t=30|k=7|tx=20|th=60|rg=1|z=15000;4|s=9999|D=1|rg=1|1=Can be used to scoop up a small amount of liquid|2=Collects water in the rain;|s=9999|t=15|rg=1|1=Contains a small amount of water|2=Can be poured out;|s=9999|t=15|rg=1|1=Contains a small amount of lava|2=Can be poured out;av|s=9999|rg=1|1=Its pretty, oh so pretty|z=100;|s=9999|rg=25|z=200;|s=9999|rg=5|z=1000;a|s=9999|r=3|rg=1|1=12% increased melee speed|2=Enables auto swing for melee weapons|z=50000;a|s=9999|r=3|rg=1|1=10% increased movement speed|z=50000;dt|s=9999|r=3|d=7|k=3|rg=1|1=Creates grass on dirt|2=Increases alchemy plant collection when used to gather|z=25000;t|s=9999|rg=100|1={$CommonItemTooltip.BurningBlock};av|s=9999|r=2|rg=1|1=May annoy others|z=100;a|s=9999|D=1|rg=1|z=10000;d|s=9999|r=3|d=20|t=27|k=7|tx=30|th=70|rg=1|z=27000;m|s=9999|r=3|d=32|t=30|k=6.5|m=21|rg=1|1=Summons a controllable ball of fire|z=125000;r|s=9999|r=3|d=30|t=14|k=2|rg=1|z=175000;d|s=9999|r=3|c=7|d=32|t=45|k=6.75|rg=1|z=125000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|1=Grows plants|z=100;a|s=9999|r=3|rg=1|1=6% reduced mana cost|z=27000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;|s=9999|rg=25|z=1000;c|s=9999|hl=90|t=17|rg=30|1=Reduced potion cooldown|z=1500;c|s=9999|hl=90|t=17|rg=30|1=Reduced potion cooldown|z=1500;4|s=9999|r=3|D=5|rg=1|1=Increases maximum mana by 40|2=6% increased magic critical strike chance|z=45000;5|s=9999|r=3|D=6|rg=1|1=Increases maximum mana by 20|2=6% increased magic damage|z=30000;6|s=9999|r=3|D=6|rg=1|1=Increases maximum mana by 20|2=6% increased magic critical strike chance|z=30000;4|s=9999|r=3|D=8|rg=1|1=7% increased melee critical strike chance|z=45000;5|s=9999|r=3|D=9|rg=1|1=7% increased melee damage|z=30000;6|s=9999|r=3|D=8|rg=1|1=7% increased melee speed|z=30000;rb|s=9999|d=8|k=1|rg=99|z=8;c|s=9999|t=25|rg=99|1=A small explosion that will destroy most tiles|2={$CommonItemTooltip.Sticky}|z=500;|s=9999|rg=1|z=5000;4v|s=9999|r=2|rg=1|1=Makes you look cool!|z=10000;4|s=9999|r=2|D=4|rg=1|1=5% increased magic damage|z=10000;4v|s=9999|rg=1|z=10000;5v|s=9999|rg=1|z=5000;6v|s=9999|rg=1|z=5000;4v|s=9999|rg=1|z=10000;4v|s=9999|rg=1|z=20000;4v|s=9999|rg=1|z=10000;5v|s=9999|rg=1|z=250000;6v|s=9999|rg=1|z=250000;4v|s=9999|rg=1|z=10000;5v|s=9999|rg=1|z=5000;6v|s=9999|rg=1|z=5000;4vt|s=9999|rg=1|z=10000;4v|s=9999|rg=1|z=10000;5v|s=9999|rg=1|z=5000;6v|s=9999|rg=1|z=5000;|s=9999|rg=5|z=10000;|s=9999|rg=5|z=2000;4|s=9999|D=2|rg=1|1=3% increased critical strike chance|z=10000;5|s=9999|D=4|rg=1|1=3% increased critical strike chance|z=5000;6|s=9999|D=3|rg=1|1=3% increased critical strike chance|z=5000;|s=9999|rg=5|z=50;4v|s=9999|rg=1|1=It smells funny...|z=1000;t|s=9999|rg=5|1=Its smiling, might be a good snack|z=3750;5v|s=9999|rg=1|z=2000;4v|s=9999|rg=1|z=10000;4v|s=9999|rg=1|z=10000;rb|s=9999|r=2|d=13|k=8|rg=99|z=100;r|s=9999|r=2|d=30|t=16|k=5|rg=1|1=This is a good idea!|z=10000;a|s=9999|rg=1|1=You are a terrible person|z=1000;4|s=9999|r=2|D=2|rg=1|1=Greatly extends underwater breathing|z=1000;v|s=9999|rg=1|z=10000;v|s=9999|rg=1|z=10000;v|s=9999|rg=1|z=10000;m|s=9999|r=3|d=35|t=20|k=5|m=14|rg=1|1=Casts a demon scythe|z=75000;d|s=9999|r=3|d=40|t=25|k=4.5|rg=1|z=200000;d|s=9999|r=3|d=34|t=22|k=5|rg=1|z=125000;t|s=9999|rg=25|1={$CommonItemTooltip.PlacementStyle}|z=1000;t|s=9999|rg=100|z=10;d|s=9999|d=14|t=31|k=6|rg=1|1=Increases mobility in water when held|2=Hold Up to descend slower|z=10000;rb|s=9999|d=9|k=3|rg=99|z=15;rc|s=9999|d=12|t=15|k=2|rg=99|z=50;d|s=9999|d=8|t=31|k=6.5|rg=1|z=1000;r|s=9999|d=9|t=25|k=3.5|rg=1|1=Allows the collection of seeds for ammo|z=10000;c|s=9999|t=15|rg=100|1={$CommonItemTooltip.WorksWhenWet}|z=10;rb|s=9999|d=4|rg=99|1=For use with Blowpipe;d|s=9999|d=10|t=20|k=5|rg=1|z=10000;a|s=9999|rg=1|1=5% increased movement speed|z=25000;c|s=9999|t=15|rg=100|1={$CommonItemTooltip.Sticky}|z=20;rc|s=9999|c=4|d=14|t=15|k=2.4|rg=99|z=60;c|s=9999|t=17|rg=20|1=Provides immunity to lava|z=1000;c|s=9999|t=17|rg=20|1=Provides life regeneration|z=1000;c|s=9999|t=17|rg=20|1=25% increased movement speed|z=1000;c|s=9999|t=17|rg=20|1=Allows you to breathe in liquids|z=1000;c|s=9999|t=17|rg=20|1=Increase defense by 8|z=1000;c|s=9999|t=17|rg=20|1=Increased mana regeneration|z=1000;c|s=9999|t=17|rg=20|1=20% increased magic damage|z=1000;c|s=9999|t=17|rg=20|1=Slows falling speed|z=1000;c|s=9999|t=17|rg=20|1=Shows the location of treasure and ore|z=1000;c|s=9999|t=17|rg=20|1=Grants invisibility and lowers the spawn rate of enemies|z=1000;c|s=9999|t=17|rg=20|1=Emits an aura of light|z=1000;c|s=9999|t=17|rg=20|1=Increases night vision|z=1000;c|s=9999|t=17|rg=20|1=Increases enemy spawn rate|z=1000;c|s=9999|t=17|rg=20|1=Attackers also take damage|z=1000;c|s=9999|t=17|rg=20|1=Allows the ability to walk on water|z=1000;c|s=9999|t=17|rg=20|1=10% increased bow damage and 20% increased arrow speed|z=1000;c|s=9999|t=17|rg=20|1=Shows the location of enemies|z=1000;c|s=9999|t=17|rg=20|1=Allows the control of gravity|z=1000;t|s=9999|rg=1|z=5000;t|s=9999|rg=25|z=80;t|s=9999|rg=25|z=80;t|s=9999|rg=25|z=80;t|s=9999|rg=25|z=80;t|s=9999|rg=25|z=80;t|s=9999|rg=25|z=80;|s=9999|rg=25|z=100;|s=9999|rg=25|z=100;|s=9999|rg=25|z=100;|s=9999|rg=25|z=100;|s=9999|rg=25|z=100;|s=9999|rg=25|z=100;|s=9999|rg=25|z=200;|s=9999|rg=25|z=50;t|s=9999|rg=2;4v|s=9999|rg=1|z=20000;|s=9999|rg=5|z=50;|s=9999|rg=1|1=Banned in most places|z=200000;5v|s=9999|rg=1|z=200000;6v|s=9999|rg=1|z=200000;|s=9999|rg=3|1=Opens one locked Gold Chest or Lock Box;t|s=9999|rg=1|z=5000;|s=9999|rg=1|1=Opens all Shadow Chests and Obsidian Lock Boxes|z=87500;w|s=9999|rg=400;|s=9999|rg=25|z=100;t|s=9999|rg=1|1=Used for crafting cloth|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|1={$CommonItemTooltip.PersonalStorage}|z=200000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=70;t|s=9999|rg=1|z=20;t|s=9999|rg=1|1=Used for brewing ale|z=600;b|s=9999|rg=20|1={$CommonItemTooltip.TipsyStats}|2=Down the hatch!|z=100;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=20;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Simple, yet refreshing.|z=10000;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;c|s=9999|t=45|rg=3|1=Summons a Goblin Army;|s=9999|rg=25|z=30;t|s=9999|rg=1|1=Used for crafting advanced items such as beds, looms and dressers|z=300;t|s=9999|r=3|rg=100|z=3500;t|s=9999|r=3|rg=100|z=5500;t|s=9999|r=3|rg=100|z=7500;d|s=9999|r=4|d=26|t=27|k=7.5|th=80|rg=1|1=Strong enough to destroy Demon Altars|z=39000;d|s=9999|r=5|d=72|t=20|k=4.5|rg=1|z=230000;t|s=9999|r=3|rg=25|z=2000;b|s=9999|rg=100|1={$CommonItemTooltip.SuffocateBlock};4|s=9999|r=4|D=3|rg=1|1=Increases maximum mana by 40|2=10% increased magic damage|3=9% increased magic critical strike chance|z=75000;4|s=9999|r=4|D=14|rg=1|1=10% increased movement speed|2=15% increased melee damage|z=75000;4|s=9999|r=4|D=5|rg=1|1=10% increased ranged damage|2=10% increased ranged critical strike chance|z=75000;5|s=9999|r=4|D=10|rg=1|1=5% increased critical strike chance|z=60000;6|s=9999|r=4|D=8|rg=1|1=10% increased movement speed and 3% increased damage|z=45000;4|s=9999|r=4|D=3|rg=1|1=Increases maximum mana by 60|2=15% increased magic damage|z=112500;4|s=9999|r=4|D=16|rg=1|1=8% increased melee critical strike chance|2=10% increased melee damage|z=112500;4|s=9999|r=4|D=6|rg=1|1=12% increased ranged damage|2=7% increased ranged critical strike chance|z=112500;5|s=9999|r=4|D=12|rg=1|1=7% increased damage|z=90000;6|s=9999|r=4|D=9|rg=1|1=10% increased critical strike chance|z=67500;t|s=9999|r=3|rg=25|z=10500;t|s=9999|r=3|rg=25|z=22000;d|s=9999|r=4|d=23|t=15|k=2.75|tx=14|rg=1|z=54000;d|s=9999|r=4|d=29|t=15|k=3|tx=17|rg=1|z=81000;d|s=9999|r=4|d=10|t=15|k=0.5|tp=110|rg=1|1=Can mine Mythril and Orichalcum|z=54000;d|s=9999|r=4|d=15|t=15|k=0.5|tp=150|rg=1|1=Can mine Adamantite and Titanium|z=81000;d|s=9999|r=4|d=33|t=15|k=4.5|tx=20|rg=1|z=108000;d|s=9999|r=4|d=20|t=15|k=0.5|tp=180|rg=1|z=108000;d|s=9999|r=5|d=50|t=45|k=6|rg=1|1=Has a chance to confuse|2=Find your inner pieces|z=144000;d|s=9999|r=4|d=45|t=26|k=5|rg=1|z=67500;t|s=9999|r=3|rg=25|z=30000;w|s=9999|rg=400;a|s=9999|rg=1|1=Displays horizontal position|z=12500;a|s=9999|r=4|rg=1|1=Grants the ability to swim|2=Greatly extends underwater breathing|z=100000;a|s=9999|r=3|rg=1|1=Shows position|2=Tells the time|z=150000;a|s=9999|r=4|rg=1|1=Negates fall damage|2=Grants immunity to fire blocks|z=60000;a|s=9999|r=4|D=2|rg=1|1=Grants immunity to knockback|2=Grants immunity to fire blocks|z=100000;t|s=9999|rg=1|1=Allows the combining of some accessories|z=100000;a|s=9999|r=4|rg=1|1=Allows the holder to double jump|2=Increases jump height|z=150000;4|s=9999|r=4|D=4|rg=1|1=Increases maximum mana by 80|2=12% increased magic damage and critical strike chance|z=150000;4|s=9999|r=4|D=22|rg=1|1=7% increased melee critical strike chance|2=14% increased melee damage|z=150000;4|s=9999|r=4|D=8|rg=1|1=14% increased ranged damage|2=10% increased ranged critical strike chance|z=150000;5|s=9999|r=4|D=16|rg=1|1=8% increased damage|z=120000;6|s=9999|r=4|D=12|rg=1|1=7% increased critical strike chance|2=5% increased movement speed|z=90000;a|s=9999|r=4|rg=1|1=Allows flight|2=The wearer can run super fast|z=100000;d|s=9999|r=4|d=49|t=25|k=6|rg=1|z=90000;a|s=9999|r=3|rg=1|1=Increases block placement range by 1|z=100000;b|s=9999|rg=100|1={$CommonItemTooltip.SuffocateBlock};t|s=9999|rg=100;5|s=9999|D=1|rg=1|1=10% increased mining speed|z=5000;6|s=9999|D=1|rg=1|1=10% increased mining speed|z=5000;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;c|s=9999|r=3|d=20|t=15|k=3|rg=99|1=Spreads the Hallow to some blocks|z=200;c|s=9999|r=3|d=20|t=15|k=3|rg=99|1=Spreads the Corruption to some blocks|z=100;t|s=9999|rg=200|1={$CommonItemTooltip.SuffocateBlock}|2={$CommonItemTooltip.CanBeExtractinated};|s=9999|r=5|t=20|rg=1|1=Summons a magical fairy|z=250000;d|s=9999|r=4|d=70|t=35|k=8|rg=1|1=Deals more damage to unhurt enemies|z=150000;t|s=9999|rg=100|z=200;t|s=9999|rg=100|z=200;t|s=9999|rg=100|z=200;t|s=9999|rg=100|z=200;t|s=9999|rg=100|z=500;t|s=9999|rg=100|z=200;t|s=9999|rg=100|z=300;r|s=9999|r=4|d=17|t=12|rg=1|1=Three round burst|2=Only the first shot consumes ammo|z=150000;r|s=9999|r=4|d=35|t=23|k=1.5|rg=1|z=60000;r|s=9999|r=4|d=39|t=20|k=2|rg=1|z=90000;|s=9999|r=4|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=150000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;w|s=9999|rg=400;t|s=9999|rg=50;r|s=9999|r=4|d=42|t=18|k=2.5|rg=1|z=120000;d|s=9999|r=4|d=61|t=21|k=6|rg=1|z=138000;d|s=9999|r=4|d=40|t=19|k=5|rg=1|z=69000;d|s=9999|r=4|d=50|t=20|k=6|rg=1|z=103500;a|s=9999|r=4|rg=1|1=Turns the holder into a werewolf at night|z=150000;d|s=9999|d=12|t=20|k=0.5|rg=1|z=1000;t|s=9999|r=3|rg=1|1={$CommonItemTooltip.UseableWhenPlaced}|2={$BuffDescription.Clairvoyance}|z=100000;t|s=9999|rg=1|z=10000;a|s=9999|r=4|rg=1|1=15% increased magic damage|z=100000;a|s=9999|r=4|rg=1|1=15% increased melee damage|z=100000;a|s=9999|r=4|rg=1|1=15% increased ranged damage|z=100000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;m|s=9999|r=5|d=42|t=12|k=2|m=5|rg=1|z=200000;m|s=9999|r=5|d=50|t=25|k=6|m=21|rg=1|1=Casts a controllable rainbow|z=200000;m|s=9999|r=4|d=28|t=9|k=2|m=6|rg=1|1=Summons a block of ice|z=500000;a|s=9999|r=5|rg=1|1=Transforms the holder into merfolk when entering water|z=375000;t|s=9999|rg=1|1=Right Click to customize attire;c|s=9999|r=3|hl=150|t=17|rg=30|z=5000;c|s=9999|r=3|hm=200|t=17|rg=30|z=500;|s=9999|rg=25|z=500;t|s=9999|rg=25|z=8000;4v|s=9999|r=2|rg=1|z=20000;5v|s=9999|r=2|rg=1|z=10000;6v|s=9999|r=2|rg=1|z=10000;r|s=9999|r=5|d=35|t=30|k=0.3|rg=1|1=Uses gel for ammo|2=Ignores 15 points of enemy Defense|z=500000;|s=9999|r=3|t=12|rg=1|z=10000;|s=9999|r=3|t=12|rg=1|z=10000;|s=9999|t=15|tr=20|rg=1|1=Places red wire|z=20000;|s=9999|t=15|tr=20|rg=1|1=Removes wire|z=20000;t|s=9999|rg=100|1=Becomes inactive when signalled|z=1000;t|s=9999|rg=100|1=Becomes active when signalled|z=1000;t|s=9999|rg=5|1={$CommonItemTooltip.WireTrigger}|z=3000;m|s=9999|r=4|d=29|t=12|k=2.5|m=8|rg=1|z=150000;rb|s=9999|r=3|d=9|k=1|rg=99|1=Creates crystal shards on impact|z=30;rb|s=9999|r=3|d=13|k=2|rg=99|1=Summons falling stars on impact|z=80;m|s=9999|r=4|d=30|t=8|k=3.75|m=6|rg=1|1=A magical returning dagger|z=250000;m|s=9999|r=4|d=35|t=7|k=5|m=5|rg=1|1=Summons rapid fire crystal shards|z=200000;m|s=9999|r=4|d=55|t=15|k=6.5|m=9|rg=1|1=Summons unholy fire balls|z=200000;|s=9999|r=3|rg=25|1=The essence of light creatures|z=1000;|s=9999|r=3|rg=25|1=The essence of dark creatures|z=1000;|s=9999|r=3|rg=25|1=Not even water can put the flame out|z=4000;t|s=9999|rg=100|1=Can be placed in water|z=150;t|s=9999|r=3|rg=1|1=Used to smelt adamantite and titanium ore|z=50000;t|s=9999|r=3|rg=1|1=Used to craft items from mythril, orichalcum, adamantite, and titanium bars|z=25000;|s=9999|rg=5|1=Sharp and magical!|z=15000;|s=9999|r=2|rg=1|1=Sometimes carried by creatures in dark deserts|z=4500;|s=9999|r=2|rg=1|1=Sometimes carried by creatures in light deserts|z=4500;t|s=9999|rg=5|1=Signals when stepped on|z=5000;|s=9999|rg=100|1=Carries signals between mechanisms|2=Use a wrench to place|z=500;|s=9999|rg=1|1=Can be enchanted|z=50000;a|s=9999|r=4|rg=1|1=Causes stars to fall after taking damage|z=100000;r|s=9999|r=5|d=25|t=7|k=1|rg=1|1=50% chance to save ammo|2=Minisharks older brother|z=350000;r|s=9999|r=4|d=24|t=45|k=6.5|rg=1|1=Fires a spread of bullets|z=250000;a|s=9999|r=4|rg=1|1=Reduces the cooldown of healing potions by 25%|z=100000;a|s=9999|r=4|rg=1|1=Increases melee knockback|2=Increases the size of melee weapons|z=100000;d|s=9999|r=4|d=44|t=28|k=4|rg=1|z=45000;t|s=9999|rg=5|1={$CommonItemTooltip.WireTrigger}|z=2000;t|s=9999|rg=5|1=Activates when signalled|z=10000;t|s=9999|rg=5;t|s=9999|rg=5|1=Signals when stepped on|z=5000;t|s=9999|rg=5|1=Signals when stepped on by a player|z=5000;t|s=9999|rg=5|1=Signals when stepped on by a player|z=5000;c|s=9999|r=3|t=45|rg=3|1=Summons The Twins;rb|s=9999|r=3|d=17|k=3|rg=99|z=40;rb|s=9999|r=3|d=12|k=4|rg=99|z=30;|s=9999|r=5|rg=25|1=The essence of pure terror|z=40000;|s=9999|r=5|rg=25|1=The essence of the destroyer|z=40000;|s=9999|r=5|rg=25|1=The essence of omniscient watchers|z=40000;d|s=9999|r=5|d=61|t=22|k=6.4|rg=1|z=230000;5|s=9999|r=5|D=15|rg=1|1=7% increased critical strike chance|z=200000;6|s=9999|r=5|D=11|rg=1|1=7% increased damage|2=8% increased movement speed|z=150000;4|s=9999|r=5|D=9|rg=1|1=15% increased ranged damage|2=8% increased ranged critical strike chance|z=250000;a|s=9999|r=4|rg=1|1=Increases length of invincibility after taking damage|z=100000;a|s=9999|r=4|rg=1|1=8% reduced mana cost|2=Automatically use mana potions when needed|z=50000;c|s=9999|r=3|t=45|rg=3|1=Summons The Destroyer;c|s=9999|r=3|t=45|rg=3|1=Summons Skeletron Prime;4|s=9999|r=5|D=5|rg=1|1=Increases maximum mana by 100|2=12% increased magic damage and critical strike chance|z=250000;4|s=9999|r=5|D=24|rg=1|1=10% increased melee damage and critical strike chance|2=10% increased melee speed|z=250000;c|s=9999|t=45|rg=3|1=Summons King Slime;d|s=9999|r=5|d=60|t=14|k=8|rg=1|1=Greetings, programs!|z=750000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;|s=9999|r=3|rg=25|1=The essence of powerful flying creatures|z=1000;a|s=9999|r=3|rg=1|1=Has a chance to record songs|z=100000;t|s=9999|rg=100;r|s=9999|r=4|d=50|t=17|k=2.5|rg=1|z=200000;d|s=9999|r=4|d=35|t=15|k=4.75|tp=200|tx=22|rg=1|1=Not to be confused with a picksaw|z=220000;t|s=9999|rg=5|1=Explodes when signalled|z=5000;t|s=9999|rg=1|1=Moves liquid to connected outlet pump when signalled;t|s=9999|rg=1|1=Receives liquid from connected input pump when signalled;t|s=9999|rg=1|1=Signals every second|z=10000;t|s=9999|rg=1|1=Signals every 3 seconds|z=10000;t|s=9999|rg=1|1=Signals every 5 seconds|z=10000;t|s=9999|rg=100;w|s=9999|rg=400;4v|s=9999|rg=1|z=150000;5v|s=9999|rg=1|z=150000;6v|s=9999|rg=1|z=150000;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=25|z=500;t|s=9999|rg=25|z=500;t|s=9999|rg=25|z=500;|s=9999|1={$CommonItemTooltip.RightClickToOpen};|s=9999|1={$CommonItemTooltip.RightClickToOpen};|s=9999|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=2|t=45|rg=3|1=Summons the Frost Legion;|s=9999|r=3|t=20|rg=1|1=Summons a pet bunny;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;d|s=9999|d=11|t=19|k=6|rg=1|z=100;d|s=9999|d=7|t=30|k=5.5|th=40|rg=1|z=50;r|s=9999|d=8|t=28|rg=1|z=100;d|s=9999|d=8|t=19|k=6|rg=1|z=100;d|s=9999|d=4|t=33|k=5.5|th=35|rg=1|z=50;r|s=9999|d=6|t=29|rg=1|z=100;d|s=9999|d=30|t=15|k=7|rg=1|z=100;d|s=9999|d=10|t=29|k=5.5|th=55|rg=1|z=50;r|s=9999|d=12|t=20|rg=1|z=100;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;|s=9999|r=3|t=20|rg=1|1=Summons a baby penguin|z=100000;d|s=9999|c=2|d=21|t=20|k=8.5|rg=1|z=50000;d|s=9999|r=8|c=16|d=105|t=20|k=6.5|rg=1|1=Deals more damage to injured foes|z=200000;d|s=9999|r=4|d=53|t=16|k=4|rg=1|1=Rapid attacks deal more damage|z=180000;t|s=9999|rg=1|z=150;d|s=9999|r=8|d=72|t=18|k=4.5|rg=1|z=500000;d|s=9999|r=8|d=70|t=32|k=4.75|rg=1|z=500000;d|s=9999|r=5|d=49|t=23|k=4.5|rg=1|1=Shoots an icy bolt|z=250000;t|s=9999|rg=1|z=300;c|s=9999|r=4|t=17|rg=3|1=Only for those who are worthy;r|s=9999|r=8|d=36|t=34|k=7|rg=1|z=400000;t|s=9999|rg=1|z=5000;t|s=9999|rg=1|z=5000;r|s=9999|r=5|c=5|d=53|t=19|k=4.7|rg=1|z=27000;m|s=9999|r=6|d=88|t=17|k=6.5|m=19|rg=1|1=Summons the Devils trident|z=500000;4|s=9999|r=5|D=10|rg=1|1=16% increased melee and ranged damage|z=250000;5|s=9999|r=5|D=20|rg=1|1=11% increased melee and ranged critical strike chance|z=200000;6|s=9999|r=5|D=13|rg=1|1=8% increased movement speed|2=10% increased melee speed|z=150000;4|s=9999|D=2|rg=1|z=1125;5|s=9999|D=2|rg=1|z=1875;6|s=9999|D=1|rg=1|z=1500;4|s=9999|D=3|rg=1|z=4500;5|s=9999|D=3|rg=1|z=7500;6|s=9999|D=2|rg=1|z=6000;4|s=9999|D=4|rg=1|z=11250;5|s=9999|D=5|rg=1|z=18750;6|s=9999|D=3|rg=1|z=15000;4|s=9999|D=5|rg=1|z=22500;5|s=9999|D=6|rg=1|z=37500;6|s=9999|D=5|rg=1|z=30000;t|s=9999|rg=100|z=375;t|s=9999|rg=100|z=750;t|s=9999|rg=100|z=1125;t|s=9999|rg=100|z=2250;t|s=9999|rg=25|z=1125;t|s=9999|rg=25|z=2250;t|s=9999|rg=25|z=4500;t|s=9999|rg=25|z=9000;a|s=9999|rg=1|1=Tells the time|z=1500;a|s=9999|rg=1|1=Tells the time|z=7500;a|s=9999|rg=1|1=Tells the time|z=15000;t|s=9999|rg=1|z=4500;t|s=9999|rg=1|z=18000;t|s=9999|rg=1|z=36000;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;4v|s=9999|rg=1|z=15000;t|s=9999|rg=1|1=Used to craft items from metal bars|z=7500;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;d|s=9999|r=4|d=52|t=20|k=6.5|rg=1|1=Shoots a beam of light|z=150000;d|s=9999|c=2|d=17|t=20|k=4.75|rg=1|1=Shoots an icy bolt|z=20000;r|s=9999|r=5|d=39|t=14|k=4.5|rg=1|1=Shoots frost arrows|z=250000;m|s=9999|r=5|d=46|t=12|k=5|m=12|rg=1|1=Shoots a stream of frost|z=200000;4|s=9999|D=1|rg=1;5|s=9999|D=1|rg=1;6|s=9999|rg=1;4|s=9999|D=1|rg=1;5|s=9999|D=2|rg=1;6|s=9999|D=1|rg=1;4|s=9999|D=1|rg=1;5|s=9999|D=1|rg=1;6|s=9999|D=1|rg=1;4|s=9999|D=2|rg=1;5|s=9999|D=3|rg=1;6|s=9999|D=2|rg=1;m|s=9999|d=15|t=37|k=3.25|m=5|rg=1|z=2000;m|s=9999|d=16|t=36|k=3.5|m=5|rg=1|z=3000;m|s=9999|d=18|t=34|k=4|m=6|rg=1|z=10000;m|s=9999|d=19|t=32|k=4.25|m=6|rg=1|z=15000;m|s=9999|d=21|t=28|k=4.75|m=7|rg=1|z=20000;m|s=9999|r=2|d=23|t=26|k=5.5|m=8|rg=1|z=30000;w|s=9999|rg=400|z=10;w|s=9999|rg=400|z=10;w|s=9999|rg=400|z=10;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|2={$CommonItemTooltip.PressUpToBooster}|z=400000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;w|s=9999|rg=400;t|s=9999|rg=100|1={$CommonItemTooltip.PreventFallDamage};w|s=9999|rg=400;|s=9999|r=3|t=20|rg=1|1=Summons a pet turtle|z=100000;4v|s=9999|r=5|rg=1|z=50000;5v|s=9999|r=5|rg=1|z=50000;d|s=9999|r=7|d=60|t=40|k=6.2|rg=1|z=700000;d|s=9999|r=8|d=85|t=18|k=6.5|rg=1|z=1000000;r|s=9999|r=8|d=60|t=20|k=4|rg=1|z=350000;r|s=9999|r=8|d=55|t=30|k=4|rg=1|1=Does extra damage on a direct hit|z=400000;r|s=9999|r=8|d=80|t=50|k=4|rg=1|1=Mines deal triple damage when armed|z=350000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100|1={$CommonItemTooltip.PreventFallDamage};t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;rb|s=9999|d=40|k=4|rg=99|1=Small blast radius. Will not destroy tiles|z=50;rb|s=9999|d=40|k=4|rg=99|1=Small blast radius. Will destroy tiles|z=250;rb|s=9999|d=65|k=6|rg=99|1=Large blast radius. Will not destroy tiles|z=100;rb|s=9999|r=2|d=65|k=6|rg=99|1=Large blast radius. Will destroy tiles|z=500;t|s=9999|rg=100|1=Increases running speed;d|s=9999|r=4|d=10|t=25|k=5|tp=110|rg=1|1=Can mine Mythril and Orichalcum|z=54000;d|s=9999|r=4|d=15|t=25|k=5|tp=150|rg=1|1=Can mine Adamantite and Titanium|z=81000;d|s=9999|r=4|d=20|t=25|k=5|tp=180|rg=1|z=108000;|s=9999|r=5|t=30|k=0.3|rg=1|1=Creates and destroys biomes when sprayed|2=Uses colored solution|z=2000000;b|s=9999|r=3|rg=99|1=Used by the Clentaminator|2=Spreads the Purity|z=1500;b|s=9999|r=3|rg=99|1=Used by the Clentaminator|2=Spreads the Hallow|z=1500;b|s=9999|r=3|rg=99|1=Used by the Clentaminator|2=Spreads the Corruption|z=1500;b|s=9999|r=3|rg=99|1=Used by the Clentaminator|2=Spreads Glowing Mushrooms|z=1500;b|s=9999|r=3|rg=99|1=Used by the Clentaminator|2=Spreads the Crimson|z=1500;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;d|s=9999|r=7|d=26|t=27|k=7.5|th=85|rg=1|1=Strong enough to destroy Demon Altars|z=400000;m|s=9999|r=7|d=35|t=25|k=1|m=12|rg=1|1=Ignores 10 points of enemy Defense|z=200000;t|s=9999|rg=1|z=5000;t|s=9999|rg=1|z=5000;t|s=9999|rg=1|z=5000;4|s=9999|D=6|rg=1|1=3% increased damage|z=50000;5|s=9999|D=7|rg=1|1=3% increased damage|z=40000;6|s=9999|D=6|rg=1|1=3% increased damage|z=30000;d|s=9999|d=22|t=25|k=5|rg=1|z=13500;r|s=9999|d=19|t=30|k=1|rg=1|z=18000;d|s=9999|d=23|t=40|k=6|th=55|rg=1|z=15000;d|s=9999|d=12|t=22|k=3.5|tp=70|rg=1|1=Able to mine Hellstone|z=18000;d|s=9999|d=22|t=32|k=6|tx=15|rg=1|z=13500;r|s=9999|d=19|t=20|k=2|rg=1|z=75000;d|s=9999|d=17|t=45|k=5.5|rg=1|z=27000;d|s=9999|d=17|t=31|k=5|rg=1|z=75000;4|s=9999|D=3|rg=1|z=50000;5|s=9999|D=3|rg=1|z=40000;6|s=9999|D=3|rg=1|z=30000;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;a|s=9999|r=8|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=5000;t|s=9999|rg=1|1=Places living wood|z=12500;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=5000;4v|s=9999|r=2|rg=1|z=15000;5v|s=9999|r=2|rg=1|z=15000;6v|s=9999|r=2|rg=1|z=15000;4v|s=9999|rg=1|z=25000;5v|s=9999|rg=1|z=25000;6v|s=9999|rg=1|z=25000;t|s=9999|rg=1|z=5000;t|s=9999|rg=1|z=5000;t|s=9999|rg=1|z=5000;4v|s=9999|rg=1|z=20000;c|s=9999|t=15|rg=50|1=Enables solid blocks to be toggled on and off|z=1000;|s=9999|t=15|tr=20|rg=1|1=Places blue wire|z=20000;|s=9999|t=15|tr=20|rg=1|1=Places green wire|z=20000;t|s=9999|rg=5|1=Signals when stepped on by a player|z=5000;t|s=9999|rg=5|1=Signals when stepped on, unless by a player|z=5000;a|s=9999|r=5|rg=1|1=Lowers shop prices by 20%|z=50000;a|s=9999|r=5|rg=1|1=Hitting enemies will sometimes drop extra coins|2=I found a coin! Its my lucky day!|z=50000;v|s=9999|r=2|rg=1|1=Having a wonderful time!|z=500;a|s=9999|r=2|rg=1|1=Allows the holder to do an improved double jump|z=50000;t|s=9999|rg=1|z=300;c|s=9999|t=20|rg=1|z=20;a|s=9999|r=6|rg=1|1=Provides life regeneration and reduces the cooldown of healing potions by 25%|z=200000;a|s=9999|r=6|rg=1|1=Turns the holder into a werewolf at night and a merfolk when entering water|z=400000;a|s=9999|r=6|rg=1|1=Causes stars to fall and increases length of invincibility after taking damage|z=100000;a|s=9999|r=4|rg=1|1=Provides the ability to walk on water & honey|z=200000;4v|s=9999|rg=1|z=250000;5v|s=9999|rg=1|z=100000;5v|s=9999|rg=1|z=20000;4|s=9999|D=2|rg=1|z=25000;4v|s=9999|rg=1|z=20000;4v|s=9999|rg=1|z=25000;4v|s=9999|rg=1|z=20000;5v|s=9999|rg=1|z=20000;6v|s=9999|rg=1|z=20000;4v|s=9999|rg=1|z=50000;5v|s=9999|rg=1|z=50000;6v|s=9999|rg=1|z=50000;4v|s=9999|rg=1|z=50000;5v|s=9999|rg=1|z=50000;6v|s=9999|rg=1|z=50000;4|s=9999|D=4|rg=1|z=25000;t|s=9999|rg=100|z=6500;d|s=9999|d=10|t=30|k=4.5|rg=1|z=1800;d|s=9999|d=4|t=25|k=2|tp=35|rg=1|z=2000;t|s=9999|rg=100;w|s=9999|rg=400;a|s=9999|r=4|rg=1|1=Grants immunity to Bleeding|z=100000;a|s=9999|r=4|rg=1|1=Grants immunity to Broken Armor|z=100000;a|s=9999|r=4|rg=1|1=Grants immunity to Poisoned|z=100000;a|s=9999|r=4|rg=1|1=Grants immunity to Darkness|z=100000;a|s=9999|r=4|rg=1|1=Grants immunity to Slow|z=100000;a|s=9999|r=4|rg=1|1=Grants immunity to Silenced|z=100000;a|s=9999|r=2|rg=1|1=Grants immunity to Cursed|z=100000;a|s=9999|r=4|rg=1|1=Grants immunity to Weakness|z=100000;a|s=9999|r=4|rg=1|1=Grants immunity to Confusion|z=100000;4|s=9999|D=1|rg=1|z=200;5|s=9999|D=1|rg=1|z=300;6|s=9999|D=1|rg=1|z=250;a|s=9999|r=5|rg=1|1=Increases melee knockback|2=12% increased melee speed|3=Enables auto swing for melee weapons|4=Increases the size of melee weapons|z=200000;a|s=9999|r=5|rg=1|1=Allows flight, super fast running|2=8% increased movement speed|z=300000;a|s=9999|r=7|rg=1|1=During daytime, grants minor increase to damage,|2=melee speed, critical strike chance, life regeneration,|3=defense, mining speed, and minion knockback|z=300000;a|s=9999|r=5|rg=1|1=During nighttime, grants minor increase to damage,|2=melee speed, critical strike chance, life regeneration,|3=defense, mining speed, and minion knockback|z=375000;a|s=9999|r=5|rg=1|1=Grants immunity to Weakness and Broken Armor|z=100000;a|s=9999|r=5|rg=1|1=Grants immunity to Poisoned and Bleeding|z=100000;a|s=9999|r=5|rg=1|1=Grants immunity to Slow and Confusion|z=100000;a|s=9999|r=5|rg=1|1=Grants immunity to Silenced and Cursed|z=100000;r|s=9999|r=6|t=8|k=2|rg=1|1=Uses coins for ammo|2=Higher valued coins do more damage|z=300000;a|s=9999|r=3|rg=1|1=Provides 7 seconds of immunity to lava|z=300000;a|s=9999|r=4|rg=1|1=Provides the ability to walk on water & honey|2=Grants immunity to fire blocks|z=300000;a|s=9999|r=7|rg=1|1=Provides the ability to walk on water, honey & lava|2=Grants immunity to fire blocks and 7 seconds of immunity to lava|3=Reduces damage from touching lava|z=500000;t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;t|s=9999|rg=100;t|s=9999|rg=1|z=200;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;d|s=9999|d=11|t=19|k=6|rg=1|z=100;d|s=9999|d=7|t=30|k=5.5|th=40|rg=1|z=50;r|s=9999|d=8|t=28|rg=1|z=100;4|s=9999|D=1|rg=1;5|s=9999|D=2|rg=1;6|s=9999|D=1|rg=1;w|s=9999|rg=400;t|s=9999|r=3|rg=1|1=Right Click to adjust trajectory|2=Use a torch or wire signal to fire|z=250000;t|s=9999|rg=25|1=Hammer to change alignment|2=Not for use with cannon|z=200;|s=9999|d=2|t=18|rg=1|z=50000;rb|s=9999|d=1|k=1.5|rg=99|z=7;t|s=9999|rg=1|1=Places bone|z=25000;t|s=9999|rg=1|1=Places leaves|z=12500;a|s=9999|r=2|rg=1|1=Allows the owner to float for a few seconds|z=50000;a|s=9999|r=5|rg=1|1=12% increased damage|z=300000;a|s=9999|r=6|rg=1|1=Increases melee knockback|2=12% increased melee damage and speed|3=Enables auto swing for melee weapons|4=Increases the size of melee weapons|z=250000;t|s=9999|rg=5|1=Explodes when stepped on or signalled|z=50000;a|s=9999|r=8|D=6|rg=1|1=Absorbs 25% of damage done to players on your team when above 25% life|2=Grants immunity to knockback|z=300000;|s=9999|r=2|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;d|s=9999|d=10|t=22|k=5|rg=1|1=You will fall slower while holding this|z=10000;t|s=9999|r=7|rg=100|1=Reacts to the light|z=7500;a|s=9999|r=8|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=3000000;rb|s=9999|d=8|k=5.75|rg=99;a|s=9999|rg=1|1=Provides extra mobility on ice|2=Ice will not break when you fall on it|z=50000;t|s=9999|r=2|rg=1|1=Rapidly launches snowballs|z=50000;t|s=9999|rg=1|z=500;a|s=9999|rg=1|1=Allows the ability to slide down walls|2=Improved ability if combined with Shoe Spikes|z=25000;4|s=9999|D=2|rg=1|z=5000;4|s=9999|D=4|rg=1|z=25000;4|s=9999|D=6|rg=1|1=5% increased critical strike chance|z=37500;5|s=9999|D=7|rg=1|1=5% increased critical strike chance|z=30000;6|s=9999|D=6|rg=1|1=5% increased critical strike chance|z=22500;4|s=9999|r=2|D=6|rg=1|1=5% increased ranged damage|z=45000;4|s=9999|r=3|D=5|rg=1|1=Increases maximum mana by 40|2=6% increased magic critical strike chance|z=45000;5|s=9999|r=3|D=6|rg=1|1=Increases maximum mana by 20|2=6% increased magic damage|z=30000;6|s=9999|r=3|D=6|rg=1|1=Increases maximum mana by 20|2=6% increased magic critical strike chance|z=30000;a|s=9999|r=7|rg=1|1=Gives a chance to dodge attacks|z=150000;r|s=9999|r=2|d=14|t=40|k=5.75|rg=1|1=Fires a spread of bullets|z=100000;t|s=9999|tr=3|rg=100|1=Can be climbed on|z=10;t|s=9999|rg=1|1=Life regen is increased when near a campfire;c|s=9999|t=17|rg=5|1=Put it on a stick and roast over a campfire|2={$CommonItemTooltip.MinorStats}|3=How many can you fit in your mouth?|z=100;|s=9999|rg=5|1=Roast it over a campfire!|z=200;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=How can I have some more of nothing?|z=200;t|s=9999|rg=5|z=1500;t|s=9999|rg=5|z=1500;t|s=9999|rg=5|z=1500;t|s=9999|rg=5|z=1500;t|s=9999|rg=100|z=60;a|s=9999|rg=1|1=Allows the ability to slide down walls|2=Improved ability if combined with Climbing Claws|z=50000;a|s=9999|r=2|rg=1|1=Allows the ability to climb walls|z=50000;a|s=9999|r=7|rg=1|1=Allows the ability to dash|2=Double tap a direction|z=150000;4|s=9999|D=3|rg=1|z=50000;5|s=9999|D=3|rg=1|z=40000;6|s=9999|D=3|rg=1|z=30000;|s=9999|rg=5|z=10000;a|s=9999|rg=1|1=Increases maximum mana by 20|2=Increases mana regeneration rate|z=50000;a|s=9999|r=4|rg=1|1=Allows the holder to do an improved double jump|2=Increases jump height|z=150000;a|s=9999|r=8|rg=1|1=Allows the ability to climb walls and dash|2=Gives a chance to dodge attacks|z=500000;c|s=9999|t=20|rg=10|1=Throw to create a climbable line of rope|z=100;r|s=9999|r=3|d=27|t=35|k=4|rg=1|1=Allows the collection of seeds for ammo|z=50000;a|s=9999|rg=1|1=Allows the holder to double jump|z=50000;rb|s=9999|d=7|k=2.2|rg=99|z=15;d|s=9999|r=2|d=23|t=21|k=4.25|rg=1|1=Shoots an enchanted sword beam|z=150000;d|s=9999|r=4|d=35|t=25|k=4.75|tp=200|tx=22|rg=1|1=Not to be confused with a hamdrill|z=220000;d|s=9999|r=4|d=33|t=35|k=5|tx=14|rg=1|z=54000;d|s=9999|r=4|d=39|t=35|k=6|tx=17|rg=1|z=81000;d|s=9999|r=4|d=43|t=35|k=7|tx=20|rg=1|z=108000;|s=9999|r=3|t=20|rg=1|1=Summons a Baby Eater of Souls|z=375000;t|s=9999|rg=1|1=Used to craft objects|z=100000;t|s=9999|rg=1|1=Used to craft objects|z=100000;t|s=9999|rg=1|1=Placing some items into the extractinator turns them into something more useful|2=Processes items from adjacent chests when signalled|z=100000;t|s=9999|rg=1|1=Used to craft objects|z=100000;t|s=9999|rg=15|z=15000;rc|s=9999|t=15|rg=5|1=Shoots confetti everywhere!|z=100;4|s=9999|r=7|D=20|rg=1|1=16% increased melee damage|2=6% increased melee critical strike chance|z=300000;4|s=9999|r=7|D=13|rg=1|1=16% increased ranged damage|2=20% chance to save ammo|z=300000;4|s=9999|r=7|D=7|rg=1|1=Increases maximum mana by 80 and 17% reduced mana cost|2=16% increased magic damage|z=300000;5|s=9999|r=7|D=18|rg=1|1=5% increased damage|2=7% increased critical strike chance|z=240000;6|s=9999|r=7|D=13|rg=1|1=8% increased critical strike chance|2=5% increased movement speed|z=180000;t|s=9999|r=7|rg=25|1=Reacts to the light|z=45000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|t=15|rg=1|1=Used with paint to color blocks|2=Can also apply coatings|z=10000;|s=9999|t=15|rg=1|1=Used with paint to color walls|2=Can also apply coatings|z=10000;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|rg=100|z=25;|s=9999|t=15|rg=1|1=Used to remove paint or coatings|2=Can sometimes collect moss|z=10000;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=200|1={$CommonItemTooltip.SuffocateBlock}|2={$CommonItemTooltip.CanBeExtractinated};t|s=9999|r=3|rg=100|z=4500;t|s=9999|r=3|rg=100|z=6500;t|s=9999|r=3|rg=100|z=8500;t|s=9999|rg=3|1=Used to make Teal Dye|z=10000;t|s=9999|rg=3|1=Used to make Green Dye|z=10000;t|s=9999|rg=3|1=Used to make Sky Blue Dye|z=10000;t|s=9999|rg=3|1=Used to make Yellow Dye|z=10000;t|s=9999|rg=3|1=Used to make Blue Dye|z=10000;t|s=9999|rg=3|1=Used to make Lime Dye|z=10000;|s=9999|rg=3|1=Used to make Pink Dye|z=10000;t|s=9999|rg=3|1=Used to make Orange Dye|z=10000;|s=9999|rg=3|1=Used to make Red Dye|z=10000;|s=9999|rg=3|1=Used to make Cyan Dye|z=10000;|s=9999|rg=3|1=Used to make Violet Dye|z=10000;|s=9999|rg=3|1=Used to make Purple Dye|z=10000;|s=9999|rg=3|1=Used to make Black Dye|z=10000;t|s=9999|rg=1|1=Used to Craft Dyes|z=50000;m|s=9999|r=2|d=9|t=12|k=0.25|m=5|rg=1|1=Shoots bees that will chase your enemy|z=100000;d|s=9999|r=7|d=80|t=14|k=5|rg=1|1=Chases after your enemy|z=350000;d|s=9999|r=3|d=30|t=20|k=5.3|rg=1|1=Summons killer bees after striking your foe|2=Causes confusion|z=100000;c|s=9999|t=15|rg=100|1={$CommonItemTooltip.CanBeExtractinated};t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;|s=9999|t=15|rg=1|1=Contains a small amount of honey|2=Can be poured out;t|s=9999|rg=1|1=Places Hives|z=25000;rc|s=9999|d=12|t=15|k=1|rg=99|1=Explodes into a swarm of bees|z=2500;a|s=9999|r=8|rg=1|1=Allows the holder to reverse gravity|2=Press Up to change gravity|z=2000000;a|s=9999|r=2|rg=1|1=Releases bees and douses the user in honey when damaged|z=100000;c|s=9999|t=45|rg=3|1=Summons the Queen Bee;c|s=9999|hl=80|t=17|rg=30|1=Improves natural healing for a short time|z=40;4|s=9999|D=1|rg=1|z=1000;5|s=9999|D=2|rg=1|z=1000;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;|s=9999|r=7|rg=1|1=Opens the jungle temple door;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1=Used for basic crafting|z=150;t|s=9999|rg=5|1=Activates when signalled|z=10000;t|s=9999|rg=5|1=Activates when signalled|z=10000;t|s=9999|rg=5|1=Activates when signalled|z=10000;t|s=9999|rg=5|1=Activates when signalled|z=10000;t|s=9999|rg=100|1={$CommonItemTooltip.ContactDamageBlock};t|s=9999|rg=5|1=Signals when stepped on by a player|z=5000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;m|s=9999|r=8|d=31|t=18|k=0.25|m=10|rg=1|1=Ignores 10 points of enemy Defense|z=500000;r|s=9999|r=8|d=40|t=30|k=1|rg=1|1=Latches on to enemies for continuous damage|z=1000000;s|s=9999|r=7|d=40|t=28|k=3|rg=1|1=Summons a Pygmy to fight for you|z=350000;a|s=9999|r=7|rg=1|1=Increases your max number of minions by 1|z=200000;4|s=9999|r=7|D=6|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 10%|3=Increases whip range by 10%|z=500000;5|s=9999|r=7|D=17|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 10%|z=500000;6|s=9999|r=7|D=12|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 10%|z=500000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=1500000;a|s=9999|r=4|rg=1|1=Allows the holder to double jump|2=Increases jump height|z=150000;a|s=9999|r=8|rg=1|1=Allows the holder to quadruple jump|2=Increases jump height|z=150000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;d|s=9999|r=3|d=19|t=22|k=5.5|rg=1|z=9000;a|s=9999|r=7|rg=1|1=Increases summon damage by 15%|2=Increases the knockback of your minions|z=400000;c|s=9999|t=15|rg=25|z=20;|s=9999|r=3|t=20|rg=1|1=Summons a Baby Skeletron Head|z=250000;|s=9999|r=3|t=20|rg=1|1=Summons a Baby Hornet|z=150000;|s=9999|r=3|t=20|rg=1|1=Summons a Tiki Spirit|z=2000000;|s=9999|r=3|t=20|rg=1|1=Summons a Pet Lizard|z=100000;t|s=9999|rg=2;t|s=9999|rg=2;t|s=9999|rg=2;t|s=9999|rg=2;t|s=9999|rg=2;m|s=9999|r=7|d=48|t=7|k=4|m=5|rg=1|1=Rapidly shoots razor sharp leaves|z=300000;rb|s=9999|r=7|d=9|k=4.5|rg=99|1=Chases after your enemy|z=50;|s=9999|r=3|t=20|rg=1|1=Summons a Pet Parrot|z=3750000;|s=9999|r=3|t=20|rg=1|1=Summons a Baby Truffle|z=450000;|s=9999|r=3|t=20|rg=1|1=Summons a Pet Sapling|z=100000;|s=9999|r=8|t=20|rg=1|1=Summons a Wisp to provide light|z=275000;t|s=9999|r=3|rg=25|z=13500;d|s=9999|r=4|d=49|t=22|k=5.5|rg=1|z=92000;d|s=9999|r=4|d=44|t=27|k=4.5|rg=1|z=60000;r|s=9999|r=4|d=37|t=22|k=1.75|rg=1|z=80000;d|s=9999|r=4|d=12|t=25|k=5|tp=130|rg=1|1=Can mine Mythril and Orichalcum|z=72000;d|s=9999|r=4|d=12|t=15|k=0.5|tp=130|rg=1|1=Can mine Mythril and Orichalcum|z=72000;d|s=9999|r=4|d=26|t=15|k=2.9|tx=15|rg=1|z=72000;t|s=9999|r=3|rg=25|z=26000;d|s=9999|r=4|d=59|t=22|k=6|rg=1|z=126500;d|s=9999|r=4|d=46|t=25|k=5.5|rg=1|z=82500;r|s=9999|r=4|d=40|t=19|k=2|rg=1|z=110000;d|s=9999|r=4|d=17|t=25|k=5|tp=165|rg=1|1=Can mine Adamantite and Titanium|z=99000;d|s=9999|r=4|d=17|t=15|k=0.5|tp=165|rg=1|1=Can mine Adamantite and Titanium|z=99000;d|s=9999|r=4|d=31|t=15|k=3.75|tx=18|rg=1|z=99000;t|s=9999|r=3|rg=25|z=34000;d|s=9999|r=4|d=61|t=20|k=6|rg=1|z=161000;d|s=9999|r=4|d=48|t=23|k=6.2|rg=1|z=105000;r|s=9999|r=4|d=43|t=17|k=2.5|rg=1|z=140000;d|s=9999|r=4|d=27|t=25|k=5|tp=190|rg=1|z=126000;d|s=9999|r=4|d=27|t=15|k=0.5|tp=190|rg=1|z=126000;d|s=9999|r=4|d=34|t=15|k=4.6|tx=21|rg=1|z=126000;4|s=9999|r=4|D=14|rg=1|1=12% increased melee damage|2=12% increased melee speed|z=75000;4|s=9999|r=4|D=5|rg=1|1=9% increased ranged damage|2=9% increased ranged critical strike chance|z=75000;4|s=9999|r=4|D=3|rg=1|1=9% increased magic damage and critical strike chance|2=Increases maximum mana by 60|z=75000;5|s=9999|r=4|D=10|rg=1|1=3% increased damage|2=2% increased critical strike chance|z=60000;6|s=9999|r=4|D=8|rg=1|1=2% increased damage|2=1% increased critical strike chance|z=45000;4|s=9999|r=4|D=19|rg=1|1=11% increased melee damage and melee speed|2=7% increased movement speed|z=112500;4|s=9999|r=4|D=7|rg=1|1=15% increased ranged critical strike chance|2=8% increased movement speed|z=112500;4|s=9999|r=4|D=4|rg=1|1=18% increased magic critical strike chance|2=Increases maximum mana by 80|z=112500;5|s=9999|r=4|D=13|rg=1|1=6% increased critical strike chance|z=90000;6|s=9999|r=4|D=10|rg=1|1=8% increased damage and 11% increased movement speed|z=67500;4|s=9999|r=4|D=23|rg=1|1=9% increased melee damage and critical strike chance|2=9% increased melee speed|z=150000;4|s=9999|r=4|D=8|rg=1|1=16% increased ranged damage|2=7% increased ranged critical strike chance|z=150000;4|s=9999|r=4|D=4|rg=1|1=16% increased magic damage and 7% increased magic critical strike chance|2=Increases maximum mana by 100|z=150000;5|s=9999|r=4|D=15|rg=1|1=4% increased damage|2=3% increased critical strike chance|z=120000;6|s=9999|r=4|D=11|rg=1|1=3% increased damage and critical strike chance|2=6% increased movement speed|z=90000;t|s=9999|r=3|rg=1|1=Used to craft items from mythril, orichalcum, adamantite, and titanium bars|z=25000;t|s=9999|r=3|rg=1|1=Used to smelt adamantite and titanium ore|z=50000;d|s=9999|r=4|d=36|t=35|k=5.5|tx=15|rg=1|z=72000;d|s=9999|r=4|d=41|t=35|k=6.5|tx=18|rg=1|z=99000;d|s=9999|r=4|d=44|t=35|k=7.5|tx=21|rg=1|z=126000;t|s=9999|r=4|rg=25|z=20000;d|s=9999|r=7|d=95|t=26|k=6|rg=1|1=Shoots a powerful orb|z=276000;d|s=9999|r=7|d=57|t=16|k=4|rg=1|1=Shoots a spore cloud|z=276000;d|s=9999|r=7|d=49|t=23|k=6.2|rg=1|1=Shoots a spore cloud|z=180000;r|s=9999|r=7|d=34|t=19|k=2.75|rg=1|z=240000;d|s=9999|r=7|d=40|t=25|k=5|tp=200|tr=1|rg=1|z=216000;d|s=9999|r=7|d=35|t=15|k=1|tp=200|rg=1|z=216000;d|s=9999|r=7|d=50|t=15|k=4.6|tx=23|rg=1|z=216000;d|s=9999|r=7|d=70|t=30|k=7|tx=23|tr=1|rg=1|z=216000;d|s=9999|r=7|d=80|t=35|k=8|th=90|tr=1|rg=1|z=216000;rb|s=9999|r=7|d=16|k=3.5|rg=99|1=Bounces back after hitting a wall|z=100;|s=9999|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=20000;|s=9999|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=20000;|s=9999|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=20000;|s=9999|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=20000;|s=9999|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=20000;|s=9999|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=20000;|s=9999|r=3|t=20|rg=1|1=Summons a Baby Dinosaur|z=375000;4v|s=9999|rg=1;m|s=9999|r=6|d=30|t=22|m=30|rg=1|1=Summons a cloud to rain down on your foes|z=175000;t|s=9999|rg=100|z=60;b|s=9999|rg=100|1={$CommonItemTooltip.SuffocateBlock};a|s=9999|r=4|rg=1|1=Causes stars to fall, releases bees and douses the user in honey when damaged|z=150000;a|s=9999|r=7|rg=1|1=10% increased critical strike chance|z=250000;a|s=9999|r=2|rg=1|1=Increases jump height|2=Releases bees and douses the user in honey when damaged|z=100000;a|s=9999|r=4|rg=1|1=Allows the holder to double jump|2=Increases jump height and negates fall damage|z=150000;a|s=9999|r=4|rg=1|1=Allows the holder to double jump|2=Increases jump height and negates fall damage|z=150000;a|s=9999|r=4|rg=1|1=Allows the holder to do an improved double jump|2=Increases jump height and negates fall damage|z=150000;a|s=9999|r=5|rg=1|1=Puts a shell around the owner when below 50% life that reduces damage by 25%|z=225000;r|s=9999|r=8|c=25|d=200|t=36|k=8|rg=1|1=Shoots a powerful, high velocity bullet|2=Right Click to zoom out|z=400000;r|s=9999|r=7|d=50|t=9|k=5.5|rg=1|1=Shoots a powerful, high velocity bullet|z=250000;m|s=9999|d=12|t=24|m=30|rg=1|1=Summons a cloud to rain blood on your foes|z=75000;t|s=9999|rg=25|z=19500;r|s=9999|r=7|d=45|t=22|k=5|rg=1|1=Shoots an explosive bolt|2=Does extra damage on a direct hit|z=350000;d|s=9999|r=7|d=65|t=40|k=6.5|rg=1|1=Shoots razor sharp flower petals at nearby enemies|z=300000;m|s=9999|r=8|d=45|t=40|k=2.5|m=20|rg=1|1=Shoots a rainbow that does continuous damage|z=1000000;rb|s=9999|r=5|d=17|k=1|rg=99|1=Explodes into deadly shrapnel|z=75;d|s=9999|r=7|d=45|t=15|k=5.2|th=90|rg=1|z=216000;t|s=9999|rg=1|1=Transports creatures to a connected teleporter when signalled|z=25000;m|s=9999|r=5|d=60|t=12|k=6.5|m=11|rg=1|1=Shoots a ball of frost|z=250000;r|s=9999|r=7|d=30|t=9|k=3.5|rg=1|1=Shoots a powerful, high velocity bullet|z=350000;m|s=9999|r=8|d=48|t=20|k=6|m=14|rg=1|z=500000;w|s=9999|rg=400|z=100;w|s=9999|rg=400|z=200;w|s=9999|rg=400|z=300;w|s=9999|rg=400|z=400;w|s=9999|rg=400|z=600;w|s=9999|rg=400|z=500;|s=9999|r=2|t=25|rg=1|1={$CommonItemTooltip.Hook}|z=45000;4v|s=9999|rg=1;4v|s=9999|rg=1|z=10000;4v|s=9999|rg=1|z=10000;4v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=37500;5|s=9999|rg=1|1=Increases maximum mana by 20|2=5% reduced mana cost|z=25000;5|s=9999|D=1|rg=1|1=Increases maximum mana by 40|2=7% reduced mana cost|z=50000;5|s=9999|D=1|rg=1|1=Increases maximum mana by 40|2=9% reduced mana cost|z=75000;5|s=9999|D=2|rg=1|1=Increases maximum mana by 60|2=11% reduced mana cost|z=100000;5|s=9999|D=2|rg=1|1=Increases maximum mana by 60|2=13% reduced mana cost|z=125000;5|s=9999|r=2|D=3|rg=1|1=Increases maximum mana by 80|2=15% reduced mana cost|z=150000;5v|s=9999|rg=1|z=250000;6v|s=9999|rg=1|z=250000;a|s=9999|rg=1|1=Increases movement speed after taking damage|z=75000;c|s=9999|r=7|t=30|rg=10|1=Permanently increases maximum life by 5|z=100000;t|s=9999|rg=1|z=300;c|s=9999|rg=3|1=Used at the Lihzahrd Altar|z=50000;d|s=9999|r=7|d=34|t=16|k=5.5|tp=210|tx=25|tr=1|rg=1|1=Capable of mining Lihzahrd Bricks|z=216000;m|s=9999|r=7|d=90|k=3|m=8|rg=1|1=Shoots a scorching ray of heat|2=Oolaa!!|z=350000;m|s=9999|r=7|c=20|d=125|t=24|k=7.5|m=18|rg=1|1=Summons a powerful boulder|z=350000;d|s=9999|r=7|d=90|t=24|k=12|rg=1|1=Punches with the force of a golem|z=350000;t|s=9999|rg=1|z=500;|s=9999|r=4|rg=1|1=Increases view range when held|z=150000;a|s=9999|r=4|rg=1|1=Increases view range for guns|2=Right Click to zoom out|z=150000;a|s=9999|r=7|rg=1|1=10% increased damage|2=8% increased critical strike chance|z=300000;rb|s=9999|r=3|d=11|k=4|rg=99|z=40;a|s=9999|r=2|rg=1|1=Generates a very subtle glow which becomes more vibrant underwater|z=50000;d|s=9999|d=15|t=22|k=5.5|rg=1|z=2000;d|s=9999|r=8|d=72|t=23|k=7.25|tx=35|th=100|tr=1|rg=1|z=500000;d|s=9999|r=5|d=50|t=25|k=5.5|rg=1|1=Shoots an icy sickle|z=250000;a|s=9999|rg=1|1=You are a terrible person|z=1000;m|s=9999|r=6|d=43|t=36|k=5.6|m=22|rg=1|1=Shoots a poison fang that pierces multiple enemies|z=200000;s|s=9999|r=4|d=8|t=28|k=2|rg=1|1=Summons a baby slime to fight for you|z=100000;rb|s=9999|r=2|d=10|k=2|rg=99|1=Inflicts poison on enemies;|s=9999|r=6|t=20|rg=1|1=Summons an eyeball spring|z=150000;|s=9999|r=6|t=20|rg=1|1=Summons a baby snowman|z=125000;m|s=9999|r=2|d=29|t=26|k=3.5|m=18|rg=1|1=Shoots a skull|z=75000;d|s=9999|r=4|d=40|t=28|k=6.5|rg=1|1=Shoots a boxing glove|z=175000;c|s=9999|t=45|rg=3|1=Summons a pirate invasion;4|s=9999|r=8|D=21|rg=1|1=6% increased melee damage|2=Enemies are more likely to target you|z=300000;5|s=9999|r=8|D=27|rg=1|1=8% increased melee damage and critical strike chance|2=Enemies are more likely to target you|z=240000;6|s=9999|r=8|D=17|rg=1|1=4% increased melee critical strike chance|2=Enemies are more likely to target you|z=180000;r|s=9999|d=10|t=19|k=1|rg=1|z=100000;d|s=9999|d=8|t=19|k=3|tp=55|rg=1|1=Ive got a bone to pick with you|z=15000;a|s=9999|r=4|rg=1|1=Increases arrow damage by 10% and greatly increases arrow speed|2=20% chance to not consume arrows|z=250000;a|s=9999|r=3|rg=1|1=Melee attacks inflict fire damage|z=100000;a|s=9999|r=3|rg=1|1=Reduces damage from touching lava|z=100000;d|s=9999|r=5|d=45|t=11|k=6.5|rg=1|z=600000;d|s=9999|d=12|t=20|k=3.5|rg=1|z=12500;|s=9999|r=7|t=20|rg=1|1=Teleports you to the position of the cursor|2=Causes the chaos state|z=500000;d|s=9999|r=6|d=57|t=25|k=5|rg=1|1=Shoots a deathly sickle|z=375000;|s=9999|r=7|rg=3|z=5000;c|s=9999|t=15|rg=25|z=750;|s=9999|rg=25|z=12;c|s=9999|t=45|rg=3|1=Summons the Brain of Cthulhu;|s=9999|r=3|rg=25|1=The blood of gods|z=4500;t|s=9999|rg=100|1=Can be placed in water|z=160;rb|s=9999|r=3|d=16|k=3|rg=99|1=Decreases targets defense|z=40;rb|s=9999|r=3|d=13|k=4|rg=99|1=Decreases targets defense|z=30;m|s=9999|r=4|d=30|t=18|k=4|m=7|rg=1|1=Sprays a shower of ichor|2=Decreases targets defense|z=200000;t|s=9999|rg=1|1=Right Click to adjust trajectory|2=Use a torch or wire signal to fire|z=500000;t|s=9999|rg=25|1=+800% jump height|z=3500;|s=9999|rg=25|1=Extremely toxic|z=1500;c|s=9999|r=4|t=17|rg=20|1=Melee and Whip attacks inflict Acid Venom on enemies|z=2500;rb|s=9999|r=3|d=19|k=4.2|rg=99|1=Inflicts target with Acid Venom|z=90;rb|s=9999|r=3|d=15|k=4.1|rg=99|1=Inflicts target with Acid Venom|z=40;a|s=9999|r=7|rg=1|1=Increases melee knockback and melee attacks inflict fire damage|2=12% increased melee damage and speed|3=Enables auto swing for melee weapons|4=Increases the size of melee weapons|z=300000;t|s=9999|rg=100|z=700;c|s=9999|t=20|rg=25|z=200;|s=9999|rg=25|z=1500;|s=9999|rg=25|z=1200;|s=9999|rg=25|z=1700;rb|s=9999|r=3|d=10|k=5|rg=99|1=Explodes into confetti on impact|z=10;rb|s=9999|r=3|d=15|k=3.6|rg=99|1=Causes confusion and bounces back after hitting a wall|z=40;rb|s=9999|r=3|d=10|k=6.6|rg=99|1=Explodes on impact|z=40;rb|s=9999|r=3|d=10|k=3.6|rg=99|1=Enemies killed will drop more money|z=40;c|s=9999|r=4|t=17|rg=20|1=Melee and Whip attacks inflict enemies with cursed flames|z=2500;c|s=9999|r=4|t=17|rg=20|1=Melee and Whip attacks set enemies on fire|z=2500;c|s=9999|r=4|t=17|rg=20|1=Melee and Whip attacks make enemies drop more gold|z=2500;c|s=9999|r=4|t=17|rg=20|1=Melee and Whip attacks decrease enemies defense|z=2500;c|s=9999|r=4|t=17|rg=20|1=Melee and Whip attacks confuse enemies|z=2500;c|s=9999|r=4|t=17|rg=20|1=Melee and Whip attacks cause confetti to appear|z=1500;c|s=9999|r=4|t=17|rg=20|1=Melee and Whip attacks poison enemies|z=2500;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1;t|s=9999|rg=1;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=25|1={$CommonItemTooltip.Platform};t|s=9999|rg=25|1={$CommonItemTooltip.Platform};t|s=9999|rg=25|1={$CommonItemTooltip.Platform};t|s=9999|rg=25|1={$CommonItemTooltip.Platform};t|s=9999|rg=25|1={$CommonItemTooltip.Platform};t|s=9999|rg=25|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.PlacementStyle};t|s=9999|rg=25|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Moosdijk}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Moosdijk}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;4v|s=9999|rg=1|z=10000;t|s=9999|r=2|rg=1|1=Used to craft weapon imbuement flasks|z=70000;t|s=9999|rg=1|1=Increases mana regeneration when placed nearby|z=2500;|s=9999|rg=100|1=Used to craft various types of ammo|z=5;t|s=9999|rg=1|1={$PaintingArtist.Wright}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Ness}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Moosdijk}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Kolf}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;m|s=9999|r=8|d=80|t=15|k=3.25|m=7|rg=1|1=Creates a shadow beam that bounces off walls|z=300000;m|s=9999|r=8|d=70|t=30|k=5|m=18|rg=1|1=Launches a ball of fire that explodes into a raging inferno|z=300000;m|s=9999|r=8|d=65|t=24|k=6|m=15|rg=1|1=Summons a lost soul to chase your foes|z=300000;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=1|1=Blows bubbles|z=40000;|s=9999|t=25|rg=1|1=Blows bubbles|z=50000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=5000;t|s=9999|rg=1|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Kolf}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Kolf}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Burczyk}|z=5000;4|s=9999|r=8|D=6|rg=1|z=375000;5|s=9999|r=8|D=14|rg=1|1=7% increased magic damage and critical strike chance|z=300000;6|s=9999|r=8|D=10|rg=1|1=8% increased magic damage|2=8% increased movement speed|z=225000;d|s=9999|r=8|d=32|t=24|k=5.25|tp=200|tr=3|rg=1|z=216000;d|s=9999|r=8|d=60|t=28|k=7|tx=30|th=90|tr=3|rg=1|z=216000;|s=9999|r=8|rg=25|z=25000;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;d|s=9999|r=8|d=100|t=15|k=9|rg=1|1=A powerful returning hammer|z=500000;4v|s=9999|rg=1|z=50000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;|s=9999|r=5|rg=1|z=125000;|s=9999|r=5|rg=1|z=125000;|s=9999|r=5|rg=1|z=125000;|s=9999|r=5|rg=1|z=125000;|s=9999|r=5|rg=1|z=125000;|s=9999|r=5|rg=1|z=125000;|s=9999|rg=1|1=For Capture the Gem. It drops when you die;|s=9999|rg=1|1=For Capture the Gem. It drops when you die;|s=9999|rg=1|1=For Capture the Gem. It drops when you die;|s=9999|rg=1|1=For Capture the Gem. It drops when you die;|s=9999|rg=1|1=For Capture the Gem. It drops when you die;|s=9999|rg=1|1=For Capture the Gem. It drops when you die;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=2500;|s=9999|r=8|rg=1|1=Unlocks a Jungle Chest in the dungeon;|s=9999|r=8|rg=1|1=Unlocks a Corruption Chest in the dungeon;|s=9999|r=8|rg=1|1=Unlocks a Crimson Chest in the dungeon;|s=9999|r=8|rg=1|1=Unlocks a Hallowed Chest in the dungeon;|s=9999|r=8|rg=1|1=Unlocks an Ice Chest in the dungeon;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Craig}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Craig}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Craig}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Craig}|z=5000;|s=9999|t=15|tr=3|rg=1|1=Used with paint to color blocks|2=Can also apply coatings|z=300000;|s=9999|t=15|tr=3|rg=1|1=Used with paint to color walls|2=Can also apply coatings|z=300000;|s=9999|t=15|tr=3|rg=1|1=Used to remove paint or coatings|2=Can sometimes collect moss|z=300000;4|s=9999|r=8|D=11|rg=1|1=Increases bow damage by 12%|2=5% increased ranged critical strike chance|z=375000;4|s=9999|r=8|D=11|rg=1|1=Increases gun damage by 12%|2=5% increased ranged critical strike chance|z=375000;4|s=9999|r=8|D=11|rg=1|1=Increases specialist ranged damage by 12%|2=These are launchers, dartguns, or anything else that doesnt shoot arrows/bullets|3=5% increased ranged critical strike chance|z=375000;5|s=9999|r=8|D=24|rg=1|1=13% increased ranged damage & critical strike chance|2=20% chance to save ammo|z=300000;6|s=9999|r=8|D=16|rg=1|1=7% increased ranged critical strike chance|2=12% increased movement speed|z=225000;t|s=9999|rg=1|1=Converts Chlorophyte Bars into Shroomite Bars|z=1000000;t|s=9999|r=7|rg=25|z=50000;r|s=9999|r=10|c=10|d=85|t=5|k=2.5|rg=1|1=66% chance to save ammo|2=It came from the edge of space|z=750000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;d|s=9999|r=8|d=29|t=16|k=2.75|rg=1|1=Rapidly throw life stealing daggers|z=1000000;|s=9999|r=8|rg=1|z=375000;d|s=9999|r=8|d=70|t=20|k=5|rg=1|1=A powerful javelin that unleashes tiny eaters|z=1000000;sS|s=9999|r=8|d=100|t=30|k=7.5|rg=1|1={$CommonItemTooltip.Sentry}|2=Summons a powerful frost hydra to spit ice at your enemies|z=1000000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Garner}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Phelps}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Duncan}|z=5000;a|s=9999|r=3|rg=1|1=Releases bees, douses the user in honey and increases movement speed when damaged|z=100000;a|s=9999|rg=1|1=The wearer can run super fast|z=50000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;a|s=9999|r=2|rg=1|1=Increases maximum mana by 20|2=Restores mana when damaged|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;|s=9999|r=5|rg=1|z=125000;a|s=9999|r=6|rg=1|1=Grants immunity to most debuffs|z=150000;a|s=9999|r=7|D=4|rg=1|1=Grants immunity to knockback and fire blocks|2=Grants immunity to most debuffs|z=250000;rb|s=9999|d=1|k=1.5|rg=99|z=7;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.AnglerFish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.AngryNimbus}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.AnomuraFungus}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Antlion}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Arapaima}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ArmoredSkeleton}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CaveBat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Bird}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BlackRecluse}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BloodFeeder}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BloodJelly}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BloodCrawler}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BoneSerpentHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Bunny}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ChaosElemental}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Mimic}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Clown}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CorruptBunny}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CorruptGoldfish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Crab}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Crimera}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CrimsonAxe}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CursedHammer}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Demon}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DemonEye}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Derpling}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.EaterofSouls}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.EnchantedSword}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ZombieEskimo}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.FaceMonster}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.FloatyGross}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.FlyingFish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.FlyingSnake}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Frankenstein}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.FungiBulb}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.FungoFish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Gastropod}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GoblinThief}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GoblinSorcerer}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GoblinPeon}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GoblinScout}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GoblinWarrior}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Goldfish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Harpy}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Hellbat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Herpling}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Hornet}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.IceElemental}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.IcyMerman}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.FireImp}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BlueJellyfish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.JungleCreeper}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.LihzahrdCrawler}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ManEater}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MeteorHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Moth}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Mummy}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MushiLadybug}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Parrot}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PigronCorruption}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Piranha}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PirateDeckhand}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Pixie}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ZombieRaincoat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Reaper}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Shark}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Skeleton}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DarkCaster}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BlueSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SnowFlinx}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.WallCreeper}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ZombieMushroom}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SwampThing}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GiantTortoise}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ToxicSludge}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.UmbrellaSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Unicorn}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.VampireBat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Vulture}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Nymph}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Werewolf}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Wolf}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SeekerHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GiantWormHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Wraith}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.WyvernHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Zombie}|z=1000;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;w|s=9999|rg=400;a|s=9999|r=2|rg=1|1=Allows the holder to double jump|z=50000;t|s=9999|rg=100|z=125;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;4|s=9999|D=2|rg=1;5|s=9999|D=3|rg=1;6|s=9999|D=2|rg=1;1;1;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|1=To me it look like a leprechaun to me|z=30000;5v|s=9999|rg=1|1=I just wanna know where the gold at!|z=30000;6v|s=9999|rg=1|1=I want the gold. I want the gold. I want the gold. Gimme the gold!|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;|s=9999|r=3|rg=10|1={$CommonItemTooltip.RightClickToOpen}|z=50000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;r|s=9999|r=8|c=6|d=60|t=9|k=2|rg=1|1=33% chance to save ammo|z=500000;rb|s=9999|d=9|k=1.5|rg=99|z=5;r|s=9999|r=8|c=6|d=85|t=25|k=5|rg=1|z=500000;rb|s=9999|d=60|k=3|rg=99|z=15;d|s=9999|d=9|t=24|k=2.25|rg=1|1=Allows the collection of hay from grass|z=6000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Stuff it all up in one bite!|z=1000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=15000;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};a|s=9999|r=7|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;|s=9999|r=3|t=20|rg=1|1=Summons a pet spider|z=100000;|s=9999|r=3|t=20|rg=1|1=Summons a pet squashling|z=100000;|s=9999|r=3|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=75000;m|s=9999|r=8|d=45|t=12|k=3|m=6|rg=1|1=Summons bats to attack your enemies|z=500000;s|s=9999|r=8|d=55|t=28|k=3|rg=1|1=Summons a raven to fight for you|z=500000;|s=9999|r=8|rg=1|1=Unlocks a Jungle Chest in the dungeon;|s=9999|r=8|rg=1|1=Unlocks a Corruption Chest in the dungeon;|s=9999|r=8|rg=1|1=Unlocks a Crimson Chest in the dungeon;|s=9999|r=8|rg=1|1=Unlocks a Hallowed Chest in the dungeon;|s=9999|r=8|rg=1|1=Unlocks an Ice Chest in the dungeon;t|s=9999|rg=1;rc|s=9999|d=13|t=19|k=6.5|rg=99|1=Best used for pranking townsfolk;|s=9999|r=3|t=20|rg=1|1=Summons a black kitty|z=100000;|s=9999|r=5|rg=1|z=125000;t|s=9999|rg=1;t|s=9999|rg=1|1={$CommonItemTooltip.PlacementStyle};t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;d|s=9999|r=2|d=20|t=15|k=5|rg=1|z=50000;d|s=9999|r=8|d=150|t=26|k=7.5|rg=1|1=Summons Pumpkin heads to attack your enemies|z=500000;d|s=9999|r=2|d=14|t=8|k=4|rg=1|z=50000;t|s=9999|rg=25|z=250;|s=9999|r=7|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=200000;a|s=9999|r=7|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;|s=9999|r=5|rg=1|z=125000;4|s=9999|r=8|D=9|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 11%|z=50000;5|s=9999|r=8|D=11|rg=1|1=Increases your max number of minions by 2|2=Increases summon damage by 11%|z=50000;6|s=9999|r=8|D=10|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 11%|3=20% increased movement speed|z=50000;r|s=9999|r=8|c=10|d=75|t=12|k=6.5|rg=1|z=500000;rb|s=9999|d=25|k=4.5|rg=99|z=15;|s=9999|r=3|t=20|rg=1|1=Summons a cursed sapling to follow you|z=100000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;c|s=9999|r=8|t=45|rg=3|1=Summons the Pumpkin Moon;a|s=9999|r=8|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 10%|z=200000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=5000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;4v|s=9999|r=3|rg=1|z=250000;a|s=9999|r=7|rg=1|1=Increases view range for guns (Right Click to zoom out)|2=10% increased ranged damage and critical strike chance|z=300000;t|s=9999|r=2|rg=1|1=Increases life regeneration when placed nearby|z=75000;a|s=9999|r=5|rg=1|1=Grants the ability to swim and greatly extends underwater breathing|2=Generates a very subtle glow which becomes more vibrant underwater|z=150000;a|s=9999|r=6|rg=1|1=Grants the ability to swim and greatly extends underwater breathing|2=Provides extra mobility on ice|3=Generates a very subtle glow which becomes more vibrant underwater|z=250000;a|s=9999|r=7|rg=1|1=Allows flight, super fast running, and extra mobility on ice|2=8% increased movement speed|z=350000;a|s=9999|r=4|rg=1|1=Allows the holder to double jump|2=Increases jump height|z=150000;a|s=9999|r=8|rg=1|1=Increases your max number of minions by 1|2=Increases your summon damage by 15% and the knockback of your minions|z=250000;a|s=9999|r=7|rg=1|1=Minor increase to damage, melee speed,|2=critical strike chance, life regeneration,|3=defense, mining speed, and minion knockback|z=400000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|2={$CommonItemTooltip.PressDownToHover}|z=400000;1;1;t|s=9999|rg=10|1={$CommonItemTooltip.RightClickToOpen}|2={$CommonItemTooltip.PlacementStyle};r|s=9999|d=20|t=38|k=3.75|rg=1|1=Dont shoot your eye out!|z=100000;a|s=9999|r=5|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;t|s=9999|rg=100;t|s=9999|rg=1|z=2500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.PlaceableOnXmasTree}|z=500;4v|s=9999|rg=1|z=10000;4v|s=9999|rg=1|z=10000;t|s=9999|rg=1|z=5000;d|s=9999|d=19|t=27|k=5.3|rg=1|z=13500;r|s=9999|r=8|d=57|t=30|k=0.425|rg=1|1=Uses gel for ammo|2=Ignores 15 points of enemy Defense|z=500000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=A cozy treat by the fireplace.|z=1000;c|s=9999|hl=80|t=17|rg=30|z=40;rc|s=9999|d=14|t=15|rg=99|z=25;|s=9999|r=8|t=20|rg=1|1=Summons a rideable reindeer|z=250000;|s=9999|r=7|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=100000;|s=9999|r=7|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=200000;d|s=9999|d=7|t=20|k=2.5|tp=55|rg=1|1=Can mine Meteorite|z=10000;d|s=9999|d=19|t=15|k=8|rg=1|z=50000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=Youll bounce off the walls!|z=2500;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=Not the gumdrop buttons!|z=2500;a|s=9999|r=2|rg=1|1=Grants immunity to Chilled and Frozen|z=50000;1|rg=1|1=Youve been naughty this year;a|s=9999|r=2|rg=1|1=Increases block placement & tool range by 1|z=50000;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;|s=9999|r=3|t=20|rg=1|1=Summons a Puppy;d|s=9999|r=8|d=86|t=23|k=7|rg=1|1=Shoots Christmas ornaments|z=500000;r|s=9999|r=8|d=38|t=4|k=1.75|rg=1|1=66% chance to save ammo|z=450000;m|s=9999|r=8|d=48|t=8|k=3.25|m=5|rg=1|1=Shoots razor sharp pine needles|z=450000;m|s=9999|r=8|d=58|k=4.5|m=9|rg=1|1=Showers an area with icicles|z=450000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;r|s=9999|r=8|d=67|t=15|k=4|rg=1|1=Launches homing missiles|z=450000;d|s=9999|r=7|d=80|t=30|k=6.7|rg=1|1=Shoots an icy spear that rains snowflakes|z=450000;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;c|s=9999|r=8|t=45|rg=3|1=Summons the Frost Moon;|s=9999|r=3|t=20|rg=1|1=Summons a Baby Grinch|z=100000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;|s=9999|rg=100|z=25;|s=9999|rg=100|z=50;|s=9999|rg=100|z=75;|s=9999|rg=3|z=10000;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;c|s=9999|r=2|t=17|rg=1|z=50000;c|s=9999|r=2|t=17|rg=1|z=50000;c|s=9999|r=2|t=17|rg=1|z=50000;c|s=9999|r=2|t=17|rg=1|z=100000;c|s=9999|r=2|t=17|rg=1|z=50000;c|s=9999|r=2|t=17|rg=1|z=50000;c|s=9999|r=2|t=17|rg=1|z=50000;c|s=9999|r=2|t=17|rg=1|z=75000;c|s=9999|r=2|t=17|rg=1|z=150000;c|s=9999|r=2|t=17|rg=1|z=50000;av|s=9999|r=5|rg=1|z=400000;4v|s=9999|rg=1|1=Fezzes are cool|z=35000;t|s=9999|rg=1|1=Right Click to customize attire;c|s=9999|r=2|t=17|rg=1|z=20000;|s=9999|t=25|rg=1|1=Used to catch critters and bait|z=2500;t|s=9999|rg=5|z=1500;t|s=9999|rg=1;t|s=9999|rg=5|z=2500;t|s=9999|r=2|rg=5|z=37500;t|s=9999|r=2|rg=5|z=20000;t|s=9999|rg=5|z=10000;t|s=9999|rg=5|z=5000;t|s=9999|r=3|rg=5|z=50000;t|s=9999|rg=5|z=7500;t|s=9999|rg=5|z=15000;t|s=9999|rg=5|z=2500;t|s=9999|rg=5|z=5000;t|s=9999|r=2|rg=5|z=2500;t|s=9999|rg=1;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=25000;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;t|s=9999|rg=5|z=2500;t|s=9999|rg=5|z=3750;t|s=9999|rg=5|z=3750;t|s=9999|rg=5|z=2500;t|s=9999|rg=5|z=2500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=2500;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=3750;t|s=9999|rg=5|z=3750;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=100000;t|s=9999|rg=5|z=7500;t|s=9999|rg=5|z=7500;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;w|s=9999|rg=400|z=75;|s=9999|r=5|rg=3|z=50000;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=25|z=500;t|s=9999|rg=1|1=Used for novelty decor crafting|z=500;t|s=9999|rg=100;t|s=9999|rg=1;t|s=9999|rg=1;d|s=9999|r=8|d=45|t=12|k=6|tp=200|tx=25|rg=1|z=200000;t|s=9999|r=6|rg=1|1={$CommonItemTooltip.UseableWhenPlaced}|2={$BuffDescription.AmmoBox}|z=100000;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;m|s=9999|r=7|d=44|t=30|k=7|m=25|rg=1|1=Shoots a venom fang that pierces multiple enemies|z=350000;4|s=9999|r=8|D=18|rg=1|1=Increases maximum mana by 60 and 13% reduced mana cost|2=10% increased magic damage and critical strike chance|z=375000;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1|1={$CommonItemTooltip.SpecialCrafting}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.SpecialCrafting}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.SpecialCrafting}|z=27000;t|s=9999|rg=1|1={$CommonItemTooltip.SpecialCrafting}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.SpecialCrafting}|z=200;t|s=9999|rg=1|1={$CommonItemTooltip.SpecialCrafting}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.SpecialCrafting}|z=100000;4|s=9999|r=8|D=23|rg=1|1=6% increased melee damage|2=Enemies are more likely to target you|z=300000;5|s=9999|r=8|D=20|rg=1|1=8% increased melee damage and critical strike chance|2=6% increased movement and melee speed|z=240000;5|s=9999|r=8|D=32|rg=1|1=5% increased melee damage and critical strike chance|2=Enemies are more likely to target you|z=240000;6|s=9999|r=8|D=18|rg=1|1=6% increased movement and melee speed|2=Enemies are more likely to target you|z=180000;t|s=9999|rg=1|1={$CommonItemTooltip.SpecialCrafting}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.SpecialCrafting}|z=100000;t|s=9999|rg=5|z=6250;t|s=9999|rg=1;t|s=9999|rg=1;|s=9999|rg=1;c|s=9999|r=4|hm=300|t=17|rg=30|z=1500;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;a|s=9999|r=3|rg=1|1=Increases tile placement speed|z=100000;a|s=9999|r=3|rg=1|1=Increases block placement & tool range by 3|z=100000;a|s=9999|r=3|rg=1|1=Automatically paints or coats placed objects|z=100000;a|s=9999|r=3|rg=1|1=Increases wall placement speed|z=100000;|s=9999|r=8|rg=25|z=25000;a|s=9999|r=4|rg=1|1=Increases pickup range for mana stars|z=150000;a|s=9999|r=5|rg=1|1=Increases pickup range for mana stars|2=15% increased magic damage|z=160000;a|s=9999|r=5|rg=1|1=Increases pickup range for mana stars|2=Restores mana when damaged|3=Increases maximum mana by 20|z=160000;4v|s=9999|rg=1|z=12500;r|s=9999|r=8|c=7|d=80|t=20|k=3|rg=1|1=Shoots a charged arrow|z=450000;t|s=9999|rg=1|z=160;t|s=9999|rg=1|z=120;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=120;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=320;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=600;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=20;t|s=9999|rg=1|z=20;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=2000;t|s=9999|rg=1|z=20;t|s=9999|rg=1|z=20;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=20;t|s=9999|rg=1|z=5000;t|s=9999|rg=1|z=300;t|s=9999|rg=100|z=50;t|s=9999|rg=100|z=50;t|s=9999|rg=100|z=50;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=1|z=200;c|s=9999|t=17|rg=20|1={$CommonItemTooltip.TipsyStats}|2=Drink too much of this, and you become karate master.|z=500;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Spicy level 5!|z=5500;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Pho sho...|z=7500;r|s=9999|r=2|d=20|t=22|k=4.5|rg=1|1=Well-timed shots reduce cooldown and increase critical strike chance|z=150000;r|s=9999|r=4|d=21|t=7|k=1.5|rg=1|1=50% chance to save ammo|2=Highly inaccurate|z=350000;w|s=9999|rg=400|z=250;|s=9999|t=20|rg=1|1=Squirts a stream of water that can turn most lights off|2=Wet players and creatures become immune to fire|z=15000;d|s=9999|c=15|d=18|t=20|k=3.5|rg=1|z=50000;t|s=9999|rg=100|z=300;4|s=9999|r=2|D=2|rg=1|1=6% increased magic damage and critical strike chance|z=30000;av|s=9999|r=8|rg=1|z=2000000;5|s=9999|D=4|rg=1|1=5% increased damage and critical strike chance|2=10% increased melee and movement speed|z=20000;5v|s=9999|rg=1|z=10000;5|s=9999|D=2|rg=1|1=6% increased magic damage and critical strike chance|2=10% reduced mana cost|z=35000;a|s=9999|r=7|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;t|s=9999|rg=1|z=10000;t|s=9999|rg=1|z=10000;t|s=9999|rg=1|z=10000;av|s=9999|r=5|rg=1|z=50000;av|s=9999|r=5|rg=1|z=50000;av|s=9999|r=5|rg=1|z=50000;av|s=9999|r=5|rg=1|z=50000;t|s=9999|rg=1|z=150;|s=9999|t=8|tf=5|rg=1|z=300;|s=9999|rg=3|z=2500;|s=9999|t=8|tf=15|rg=1|z=12000;|s=9999|r=2|t=8|tf=30|rg=1|z=50000;|s=9999|t=8|tf=20|rg=1|z=120000;|s=9999|r=3|t=8|tf=50|rg=1|z=1000000;|s=9999|r=2|t=8|tf=35|rg=1|z=200000;|s=9999|r=2|t=8|tf=40|rg=1|z=350000;|s=9999|rg=3|z=2500;|s=9999|rg=3|z=3750;|s=9999|rg=3|z=3750;|s=9999|rg=3|z=3750;|s=9999|rg=3|z=3750;|s=9999|rg=3|1=Its colorful scales could sell well.|z=7500;|s=9999|rg=3|z=7500;|s=9999|rg=3|z=15000;|s=9999|rg=3|z=3750;|s=9999|rg=3|z=7500;|s=9999|r=2|rg=3|z=12500;|s=9999|r=3|rg=3|1=Quite shiny. This will probably sell well.|z=500000;|s=9999|rg=3|z=3750;|s=9999|r=3|rg=3|z=50000;|s=9999|rg=3|z=7500;|s=9999|r=2|rg=3|z=25000;|s=9999|rg=3|z=7500;c|s=9999|hl=120|t=17|rg=30|z=7500;|s=9999|r=2|rg=3|z=7500;|s=9999|rg=3|z=7500;|s=9999|r=4|rg=3|z=150000;|s=9999|rg=3|z=7500;|s=9999|rg=3|z=7500;d|s=9999|r=3|d=24|t=24|k=6|th=70|rg=1|z=75000;|s=9999|rg=3|z=12500;c|s=9999|t=17|rg=20|1=Increases mining speed by 25%|z=1000;c|s=9999|t=17|rg=20|1=Increases pickup range for life hearts|z=1000;c|s=9999|t=17|rg=20|1=Decreases enemy spawn rate|z=1000;c|s=9999|t=17|rg=20|1=Increases placement speed and range|z=1000;c|s=9999|t=17|rg=20|1=Increases knockback|z=1000;c|s=9999|t=17|rg=20|1=Lets you move swiftly in liquids|z=1000;c|s=9999|t=17|rg=20|1=Increases your max number of minions by 1|z=1000;c|s=9999|t=17|rg=20|1=Allows you to see nearby danger sources|z=1000;d|s=9999|d=35|t=35|k=8|rg=1|z=50000;d|s=9999|r=7|c=20|d=70|t=20|k=6.5|rg=1|z=50000;d|s=9999|r=2|d=19|t=20|k=4.25|rg=1|z=25000;w|s=9999|rg=400;t|s=9999|rg=10|1={$CommonItemTooltip.RightClickToOpen}|z=5000;t|s=9999|r=2|rg=10|1={$CommonItemTooltip.RightClickToOpen}|z=25000;t|s=9999|r=3|rg=10|1={$CommonItemTooltip.RightClickToOpen}|z=100000;c|s=9999|t=15|rg=1|1={$CommonItemTooltip.CanBeExtractinated};c|s=9999|t=15|rg=1|1={$CommonItemTooltip.CanBeExtractinated};c|s=9999|t=15|rg=1|1={$CommonItemTooltip.CanBeExtractinated};t|s=9999|tr=5|rg=100|1=Hammer end piece to change bumper style|2=Hammer or signal intersections to change direction;d|s=9999|r=3|d=16|t=22|k=3|tp=59|rg=1|z=75000;d|s=9999|r=3|d=13|t=15|k=2.25|tx=14|rg=1|z=75000;|s=9999|rg=1|1=Lets ride the rails|z=1000;c|s=9999|t=17|rg=20|1=20% chance to save ammo|z=1000;c|s=9999|t=17|rg=20|1=Increases max life by 20%|z=1000;c|s=9999|t=17|rg=20|1=Reduces damage taken by 10%|z=1000;c|s=9999|t=17|rg=20|1=Increases critical chance by 10%|z=1000;c|s=9999|t=17|rg=20|1=Ignites nearby enemies|z=1000;c|s=9999|t=17|rg=20|1=Increases damage by 10%|z=1000;c|s=9999|t=30|rg=20|1=Teleports you home|z=1000;c|s=9999|t=17|rg=20|1=Teleports you to a random location|z=1000;c|s=9999|t=15|rg=20|1=Throw this to make someone fall in love|z=200;c|s=9999|t=15|rg=20|1=Throw this to make someone smell terrible|z=200;c|s=9999|t=17|rg=20|1=Increases fishing power by 15|z=1000;c|s=9999|t=17|rg=20|1=Detects hooked fish|z=1000;c|s=9999|t=17|rg=20|1=Increases chance to get a crate|z=1000;t|s=9999|rg=25|z=80;|s=9999|rg=25|z=100;c|s=9999|t=17|rg=20|1=Reduces damage from cold sources|z=1000;|s=9999|r=3|t=20|rg=1|1={$CommonItemTooltip.Hook}|z=20000;4|s=9999|r=3|D=4|rg=1|1=Increases summon damage by 4%|2=Increases your max number of minions by 1|z=20000;5|s=9999|r=3|D=5|rg=1|1=Increases summon damage by 4%|2=Increases your max number of minions by 1|z=30000;6|s=9999|r=3|D=4|rg=1|1=Increases summon damage by 5%|z=25000;s|s=9999|r=3|d=12|t=22|k=2|rg=1|1=Summons a hornet to fight for you|z=35000;s|s=9999|r=3|d=17|t=36|k=2|rg=1|1=Summons an imp to fight for you|z=27000;sS|s=9999|r=4|d=26|t=30|k=7.5|rg=1|1={$CommonItemTooltip.Sentry}|2=Summons a spider queen to spit eggs at your enemies|z=250000;4|s=9999|D=1|rg=1|1=Increases fishing power by 5|z=50000;5|s=9999|D=2|rg=1|1=Increases fishing power by 5|z=50000;6|s=9999|D=1|rg=1|1=Increases fishing power by 5|z=50000;4|s=9999|r=4|D=5|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 5%|z=37500;5|s=9999|r=4|D=8|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 5%|z=37500;6|s=9999|r=4|D=7|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 6%|z=37500;a|s=9999|rg=1|1=Fishing line will never break|z=50000;a|s=9999|rg=1|1=Increases fishing power by 10|z=50000;a|s=9999|rg=1|1=Decreases chance of bait consumption|z=50000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;4v|s=9999|rg=1|z=50000;5v|s=9999|rg=1|z=50000;6v|s=9999|rg=1|z=50000;|s=9999|r=3|t=20|rg=1|1=Summons a pet Zephyr Fish|z=150000;|s=9999|t=8|tf=22|rg=1|z=156000;|s=9999|r=3|t=8|tf=45|rg=1|1={$CommonItemTooltip.LavaFishing}|z=500000;a|s=9999|rg=1|1=Increases jump speed and allows auto-jump|2=Increases fall resistance|z=50000;d|s=9999|r=3|d=70|t=20|k=8|rg=1|z=50000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Wheres the chips!?|z=2500;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Barbecue it, boil it, broil it, bake it...|z=7500;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Its raw! Its exotic!|z=2500;|s=9999|r=8|t=20|rg=1|1=Summons a rideable Bunny mount|z=250000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable Pigron mount|z=250000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable Slime mount|z=250000;|s=9999|rg=25|z=2500;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400|z=50;t|s=9999|rg=100|z=50;c|s=9999|t=15|rg=3|z=175000;c|s=9999|t=15|rg=3|z=175000;c|s=9999|t=15|rg=3|z=175000;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1|z=25000;t|s=9999|rg=1|z=25000;t|s=9999|rg=1|z=25000;t|s=9999|rg=1|z=25000;t|s=9999|rg=1|z=25000;t|s=9999|rg=1|z=25000;t|s=9999|rg=1|z=25000;t|s=9999|rg=1|z=25000;|s=9999|rg=2|1=Caught in Underground & Caverns;|s=9999|rg=2|1=Caught in Honey;|s=9999|rg=2|1=Caught in Jungle Surface;|s=9999|rg=2|1=Caught in Sky Lakes;|s=9999|rg=2|1=Caught in Corruption;|s=9999|rg=2|1=Caught in Surface & Underground;|s=9999|rg=2|1=Caught in Surface;|s=9999|rg=2|1=Caught in Corruption;|s=9999|rg=2|1=Caught in Sky Lakes & Surface;|s=9999|rg=2|1=Caught in Sky Lakes & Surface;|s=9999|rg=2|1=Caught in Caverns;|s=9999|rg=2|1=Caught in Sky Lakes & Surface;|s=9999|rg=2|1=Caught in Caverns;|s=9999|rg=2|1=Caught in Crimson;|s=9999|rg=2|1=Caught in Underground & Caverns;|s=9999|rg=2|1=Caught in Underground Hallow;|s=9999|rg=2|1=Caught in Underground Tundra;|s=9999|rg=2|1=Caught in Surface Tundra;|s=9999|rg=2|1=Caught in Surface Hallow;|s=9999|rg=2|1=Caught in Underground & Caverns;|s=9999|rg=2|1=Caught in Surface Tundra;|s=9999|rg=2|1=Caught in Hallow;|s=9999|rg=2|1=Caught in Caverns;|s=9999|rg=2|1=Caught in Sky Lakes;|s=9999|rg=2|1=Caught in Surface;|s=9999|rg=2|1=Caught in Glowing Mushroom Fields;|s=9999|rg=2|1=Caught in Sky Lakes;|s=9999|rg=2|1=Caught in Crimson;|s=9999|rg=2|1=Caught in Underground & Caverns;|s=9999|rg=2|1=Caught in Surface;|s=9999|rg=2|1=Caught in Ocean;|s=9999|rg=2|1=Caught in Ocean;|s=9999|rg=2|1=Caught in Caverns;|s=9999|rg=2|1=Caught in Jungle Surface;|s=9999|rg=2|1=Caught in Underground Tundra;|s=9999|rg=2|1=Caught in Corruption;|s=9999|rg=2|1=Caught in Jungle;|s=9999|rg=2|1=Caught in Surface Forest;|s=9999|rg=2|1=Caught in Jungle Surface;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=150000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable Turtle mount|z=250000;t|s=9999|tr=2|rg=5|1=Signals when a minecart travels over it|2=Not for use on slopes|z=5000;4v|s=9999|rg=1|z=37500;a|s=9999|r=4|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=25000;4v|s=9999|rg=1|z=50000;5v|s=9999|rg=1|z=50000;6v|s=9999|rg=1|z=50000;av|s=9999|r=5|rg=1|z=50000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable Bee mount|z=250000;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;4|s=9999|D=1|rg=1;5|s=9999|D=1|rg=1;6|s=9999|D=1|rg=1;4|s=9999|D=1|rg=1;5|s=9999|D=1|rg=1;6|s=9999|D=1|rg=1;r|s=9999|d=6|t=29|rg=1|z=100;d|s=9999|d=4|t=33|k=5.5|th=35|rg=1|z=50;d|s=9999|d=8|t=19|k=6|rg=1|z=100;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;s|s=9999|r=5|d=24|t=36|k=2|rg=1|1=Summons twins to fight for you|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;s|s=9999|r=4|d=26|t=36|k=3|rg=1|1=Summons spiders to fight for you|z=50000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;s|s=9999|r=5|d=40|t=36|k=6|rg=1|1=Summons pirates to fight for you|z=50000;|s=9999|r=3|t=20|rg=1|1={$CommonItemTooltip.Hook}|z=20000;rc|s=9999|d=60|t=45|k=8|rg=99|1=A small explosion that will not destroy tiles|2={$CommonItemTooltip.Sticky}|z=75;|s=9999|r=3|t=20|rg=1|1=Summons a pet faun|z=100000;4v|s=9999|rg=1|z=37500;t|s=9999|rg=1|z=50000;rc|s=9999|d=23|t=40|k=7|rg=99|1=A small explosion that puts enemies on fire|2=Lights nearby area on fire for a while|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;|s=9999|r=4|rg=25|z=2500;d|s=9999|r=4|d=25|t=20|k=6|rg=1|1=Rapid attacks deal more damage|z=10000;a|s=9999|r=8|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|2=Allows quick travel in water|z=400000;|s=9999|t=12|rg=1|1=Squirts a stream of slime that can turn most lights on|2=Slimed players and creatures take more damage from fire|z=15000;d|s=9999|r=8|d=66|t=20|k=4.5|rg=1|1=Spews homing bubbles|2=Right Click to toggle modes|z=250000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;s|s=9999|r=8|d=50|t=36|k=2|rg=1|1=Summons sharknados to fight for you|z=250000;m|s=9999|r=8|d=85|t=40|k=5|m=20|rg=1|1=Casts fast moving razorwheels|z=250000;m|s=9999|r=8|d=70|t=9|k=3|m=5|rg=1|1=Rapidly shoots forceful bubbles|z=250000;r|s=9999|r=8|d=53|t=24|k=2|rg=1|1=Shoots 5 arrows at a time|z=250000;t|s=9999|rg=5|1={$CommonItemTooltip.PlacementStyle}|z=2500;t|s=9999|rg=5|1={$CommonItemTooltip.PlacementStyle}|z=5000;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|r=3|rg=3|z=500000;c|s=9999|rg=5|z=500;c|s=9999|r=2|rg=5|z=1500;c|s=9999|r=3|rg=5|z=5000;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|1=Right Click to place item on weapon rack|z=250;t|s=9999|rg=1|z=50000;t|s=9999|rg=100;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=30000;t|s=9999|tr=2|rg=5|1=Hammer or signal to change direction|z=5000;t|s=9999|rg=5|z=1250;t|s=9999|rg=1;avt|s=9999|r=4|rg=1|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};d|s=9999|d=8|t=20|k=6|rg=1|z=100;d|s=9999|d=4|t=33|k=5.5|th=35|rg=1|z=50;r|s=9999|d=6|t=29|rg=1|z=100;t|s=9999|rg=1|z=500;s|s=9999|r=8|d=36|t=36|k=2|rg=1|1=Summons a UFO to fight for you|z=500000;m|s=9999|r=5|d=50|k=4.5|m=9|rg=1|1=Showers meteors|z=100000;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;c|s=9999|t=17|rg=20|z=1000;4|s=9999|r=10|D=14|rg=1|1=16% increased ranged damage|2=7% increased ranged critical strike chance|z=350000;5|s=9999|r=10|D=28|rg=1|1=12% increased ranged damage and critical strike chance|2=25% chance to save ammo|z=700000;6|s=9999|r=10|D=20|rg=1|1=8% increased ranged damage and critical strike chance|2=10% increased movement speed|z=525000;4|s=9999|r=10|D=14|rg=1|1=Increases maximum mana by 60 and 15% reduced mana cost|2=7% increased magic damage and critical strike chance|z=350000;5|s=9999|r=10|D=18|rg=1|1=9% increased magic damage and critical strike chance|z=700000;6|s=9999|r=10|D=14|rg=1|1=10% increased magic damage|2=10% increased movement speed|z=525000;4|s=9999|r=10|D=24|rg=1|1=26% increased melee critical strike chance|2=Grants minor life regeneration|3=Enemies are more likely to target you|z=350000;5|s=9999|r=10|D=34|rg=1|1=29% increased melee damage|2=Grants minor life regeneration|3=Enemies are more likely to target you|z=700000;6|s=9999|r=10|D=20|rg=1|1=15% increased movement and melee speed|2=Grants minor life regeneration|3=Enemies are more likely to target you|z=525000;|s=9999|r=8|rg=10;c|s=9999|r=8|t=45|rg=3|1=Summons a Solar Eclipse;|s=9999|r=8|t=20|rg=1|1=Summons a rideable Drill mount|2=Left Click to mine blocks, Right Click to mine walls|z=250000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable UFO mount|z=250000;a|s=9999|r=8|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=625000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable Scutlix mount|z=250000;d|s=9999|r=10|d=100|t=25|k=6|tx=27|tr=4|z=300000;d|s=9999|r=10|d=80|t=15|k=4|tx=27|tr=3|z=300000;d|s=9999|r=10|d=50|t=15|k=0.5|tp=225|tr=2|rg=1|z=350000;d|s=9999|r=10|d=110|t=30|k=7|th=100|tr=4|z=400000;d|s=9999|r=10|d=80|t=12|k=5.5|tp=225|tr=4|rg=1|z=350000;d|s=9999|r=10|d=100|t=25|k=6|tx=27|tr=4|z=300000;d|s=9999|r=10|d=80|t=15|k=4|tx=27|tr=3|z=300000;d|s=9999|r=10|d=50|t=15|k=0.5|tp=225|tr=2|rg=1|z=350000;d|s=9999|r=10|d=110|t=30|k=7|th=100|tr=4|z=400000;d|s=9999|r=10|d=80|t=12|k=5.5|tp=225|tr=4|rg=1|z=350000;d|s=9999|r=10|d=100|t=25|k=6|tx=27|tr=4|z=300000;d|s=9999|r=10|d=80|t=15|k=4|tx=27|tr=3|z=300000;d|s=9999|r=10|d=50|t=15|k=0.5|tp=225|tr=2|rg=1|z=350000;d|s=9999|r=10|d=110|t=30|k=7|th=100|tr=4|z=400000;d|s=9999|r=10|d=80|t=12|k=5.5|tp=225|tr=4|rg=1|z=350000;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;m|s=9999|r=8|d=60|t=20|k=2|m=6|rg=1|z=500000;r|s=9999|r=8|d=40|t=12|k=2|rg=1|z=500000;r|s=9999|r=8|d=45|t=21|k=3|rg=1|z=500000;d|s=9999|r=8|d=35|t=25|k=4.75|tp=230|tr=11|rg=1|z=500000;a|s=9999|rg=1|1=Creates measurement lines on screen for block placement|z=10000;|s=9999|r=7|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=125000;4v|s=9999|rg=1;4v|s=9999|rg=1;4v|s=9999|rg=1|z=50000;5v|s=9999|rg=1|z=50000;6v|s=9999|rg=1|z=50000;4v|s=9999|rg=1|z=50000;5v|s=9999|rg=1|z=50000;6v|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;4v|s=9999|rg=1|z=100000;4v|s=9999|rg=1|z=100000;5v|s=9999|rg=1|z=100000;5v|s=9999|rg=1|z=100000;t|s=9999|rg=100;w|s=9999|rg=400;4v|s=9999|r=3|rg=1|z=50000;c|s=9999|r=3|t=17|rg=1|z=300000;|s=9999|r=3|rg=3|z=75000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=20000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=20000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=20000;t|s=9999|rg=100|z=100;|s=9999|r=3|rg=3|z=75000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=2|rg=3|z=37500;|s=9999|r=2|rg=3|z=37500;|s=9999|r=3|rg=3|z=75000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=3|rg=3|z=75000;d|s=9999|r=8|d=100|t=20|k=4.5|rg=1|z=500000;|s=9999;m|s=9999|r=8|d=100|t=20|k=2|m=14|rg=1|z=500000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=3|rg=3|z=75000;c|s=9999|t=15|rg=99|1=Spreads the Crimson|z=100;|s=9999|rg=25|z=50;r|s=9999|r=3|d=23|t=23|k=3|rg=1|1=Wooden arrows turn into a column of bees|z=100000;t|s=9999|r=3|rg=3|z=500000;t|s=9999|r=3|rg=3|z=500000;t|s=9999|r=3|rg=3|z=500000;t|s=9999|r=3|rg=3|z=500000;t|s=9999|r=3|rg=3|z=500000;t|s=9999|r=3|rg=3|z=500000;t|s=9999|r=3|rg=3|z=500000;c|s=9999|t=40|rg=99|1=A large explosion that will destroy most tiles|2={$CommonItemTooltip.Sticky}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.AngryTrapper}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ArmoredViking}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BlackSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BlueArmoredBones}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CultistArcherBlue}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CultistDevote}|z=1000;t|s=9999|1={$CommonItemTooltip.BannerBonus}{$NPCName.None}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BoneLee}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Clinger}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CochinealBeetle}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CorruptPenguin}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CorruptSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Corruptor}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Crimslime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CursedSkull}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CyanBeetle}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DevourerHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DiabolistRed}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DoctorBones}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DungeonSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DungeonSpirit}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ElfArcher}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ElfCopter}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Eyezor}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Flocko}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Ghost}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GiantBat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GiantCursedSkull}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GiantFlyingFox}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GingerbreadMan}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GoblinArcher}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GreenSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.HeadlessHorseman}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.HellArmoredBones}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Hellhound}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.HoppinJack}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.IceBat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.IceGolem}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.IceSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.IchorSticker}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.IlluminantBat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.IlluminantSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.JungleBat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.JungleSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Krampus}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.LacBeetle}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Lavabat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.LavaSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BrainScrambler}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MartianDrone}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MartianEngineer}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GigaZapper}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GrayGrunt}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MartianOfficer}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.RayGunner}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ScutlixRider}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MartianTurret}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MisterStabby}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MotherSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Necromancer}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Nutcracker}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Paladin}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Penguin}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Pinky}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Poltergeist}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PossessedArmor}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PresentMimic}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PurpleSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.RaggedCaster}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.RainbowSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Raven}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.RedSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.RuneWizard}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.RustyArmoredBonesAxe}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Scarecrow1}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Scutlix}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SkeletonArcher}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SkeletonCommando}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SkeletonSniper}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Slimer}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Snatcher}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SnowBalla}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SnowmanGangsta}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SpikedIceSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SpikedJungleSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Splinterling}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Squid}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.TacticalSkeleton}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.TheGroom}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Tim}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.UndeadMiner}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.UndeadViking}|z=1000;t|s=9999|1={$CommonItemTooltip.BannerBonus}{$NPCName.CultistArcherWhite}|z=1000;t|s=9999|1={$CommonItemTooltip.BannerBonus}{$NPCName.None}|z=1000;t|s=9999|1={$CommonItemTooltip.BannerBonus}{$NPCName.None}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.YellowSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Yeti}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ZombieElf}|z=1000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|2=In loving memory|z=5000;t|s=9999|tr=3|rg=100|1=Can be climbed on;c|s=9999|rg=20|1=Teleports you to a party member|2=Click their head on the fullscreen map|z=1000;a|s=9999|r=4|rg=1|1=15% increased summon damage|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.UseableWhenPlaced}|2={$BuffDescription.Bewitched}|z=100000;t|s=9999|rg=1|1=33% chance to not consume potion crafting ingredients|z=100000;c|s=9999|hl=70|t=17|rg=30|1=Side Effects May Include:|2=-Unpredictable Healing|3=-Inconsistent Potion Sickness|4=-Brief Periods of Inexplicable Invulnerability|5=It looks and smells terrible|z=500;c|s=9999|t=15|rg=100|1=Exposes nearby treasure|z=150;rb|s=9999|d=8|k=2.5|rg=99|z=15;t|s=9999|rg=100|1=Emits a deathly glow|z=100;c|s=9999|t=20|rg=10|1=Throw to create a climbable line of vine rope;m|s=9999|r=5|d=35|t=12|k=2.5|m=10|rg=1|1=Drains life from enemies|z=400000;r|s=9999|r=5|d=28|t=22|k=3.5|rg=1|z=400000;r|s=9999|r=5|d=52|t=38|k=5.5|rg=1|z=400000;rb|s=9999|r=3|d=14|k=3.5|rg=99|1=Bounces between enemies|z=30;rb|s=9999|r=3|d=9|k=2.2|rg=99|1=Drops cursed flames on the ground|z=30;rb|s=9999|r=3|d=7|k=2.5|rg=99|1=Bursts into multiple darts|z=30;d|s=9999|r=5|d=59|t=14|k=3.25|rg=1|z=400000;d|s=9999|r=5|d=60|t=8|k=6|rg=1|z=400000;m|s=9999|r=5|d=43|t=24|k=8|m=40|rg=1|1=Summons a wall of cursed flames|z=400000;a|s=9999|r=6|rg=1|1=Enemies are less likely to target you|2=5% increased damage and critical strike chance|z=400000;a|s=9999|r=5|D=8|rg=1|1=Enemies are more likely to target you|z=400000;a|s=9999|r=7|rg=1|1=Flowers grow on the grass you walk on|z=300000;d|s=9999|r=5|d=50|t=23|k=6|rg=1|z=500000;r|s=9999|r=3|d=22|t=13|k=5.5|rg=1|1=Wooden arrows turn into flaming bats|z=125000;|s=9999|r=6|t=20|rg=1|1={$CommonItemTooltip.Hook}|z=300000;|s=9999|r=6|t=20|rg=1|1={$CommonItemTooltip.Hook}|z=300000;|s=9999|r=6|t=20|rg=1|1={$CommonItemTooltip.Hook}|z=300000;|s=9999|r=6|t=20|rg=1|1={$CommonItemTooltip.Hook}|z=300000;|s=9999|r=9|rg=3|1={$CommonItemTooltip.DevItem}|z=150000;|s=9999|r=2|rg=3|z=37500;|s=9999|r=2|rg=3|z=37500;|s=9999|r=2|rg=3|z=37500;|s=9999|r=2|rg=3|z=37500;r|s=9999|r=6|d=38|t=19|k=2.25|rg=1|1=Shoots arrows from the sky|z=400000;d|s=9999|r=6|d=40|t=15|k=4.5|rg=1|1=Throws a controllable flying knife|z=400000;|s=9999|r=7|t=12|tr=2|rg=1|1=Contains an endless amount of water|2=Can be poured out|z=500000;|s=9999|r=7|t=12|tr=2|rg=1|1=Capable of soaking up an endless amount of water|z=500000;a|s=9999|r=5|rg=1|1=Increases coin pickup range|z=50000;a|s=9999|r=5|rg=1|1=Increases coin pickup range|2=Hitting enemies will sometimes drop extra coins|z=100000;a|s=9999|r=6|rg=1|1=Increases coin pickup range|2=Hitting enemies will sometimes drop extra coins|3=Lowers shop prices by 20%|z=150000;a|s=9999|r=3|rg=1|1=Displays weather, moon phase, and fishing information|z=150000;a|s=9999|rg=1|1=Displays the weather|z=50000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=2|rg=3|z=37500;|s=9999|r=2|rg=3|z=37500;|s=9999|r=3|rg=3|z=75000;|s=9999|r=3|t=20|rg=1|1=Summons a magic lantern that exposes nearby treasure|z=100000;avt|s=9999|r=4|rg=1|z=100000;t|s=9999|rg=100|z=250;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;m|s=9999|r=5|d=25|t=33|k=3|m=13|rg=1|1=Summons a massive crystal spike|2=Ignores 10 points of enemy Defense|z=400000;r|s=9999|r=5|c=3|d=47|t=20|k=4.5|rg=1|1=Shoots Shadowflame Arrows|z=100000;m|s=9999|r=5|c=3|d=32|t=21|k=3.75|m=6|rg=1|1=Summons Shadowflame tentacles to strike your foes|z=100000;d|s=9999|r=5|c=3|d=43|t=12|k=5.75|rg=1|1=Inflicts Shadowflame on hit|z=100000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=5000;|s=9999|r=3|t=20|rg=1|1=Summons a Baby Face Monster|z=375000;a|s=9999|r=5|rg=1|1=Increases block & wall placement speed|2=Increases block placement & tool range by 3|3=Automatically paints or coats placed objects|z=200000;|s=9999|t=20|rg=1|1=Summons a heart to provide light|z=75000;d|s=9999|r=10|d=200|t=14|k=6.5|rg=1|z=1000000;t|s=9999|r=7|rg=1|1=Allows time to fast forward to dawn one day per week|2=Instantly reusable after certain celestial events|z=150000;d|s=9999|r=10|d=170|t=16|k=6.5|rg=1|1=Causes stars to rain from the sky|z=1000000;t|s=9999|rg=100;w|s=9999|rg=400;a|s=9999|rg=1|1=Allows the collection of Vine Rope from vines|z=25000;m|s=9999|c=10|d=14|t=26|m=2|rg=1|1=Shoots a small spark|z=10000;t|s=9999|r=3|rg=1|z=500000;t|s=9999|r=3|rg=1|z=500000;t|s=9999|r=3|rg=1|z=500000;t|s=9999|r=3|rg=1|z=500000;t|s=9999|r=3|rg=1|z=500000;t|s=9999|r=3|rg=1|z=500000;t|s=9999|r=3|rg=1|z=500000;t|s=9999|tr=3|rg=100|1=Can be climbed on|z=10;t|s=9999|tr=3|rg=100|1=Can be climbed on|z=10;c|s=9999|t=20|rg=10|1=Throw to create a climbable line of silk rope|z=100;c|s=9999|t=20|rg=10|1=Throw to create a climbable line of web rope|z=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;a|s=9999|rg=1|1=Detects enemies around you|z=25000;|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|2=Requires a Golden Key|z=20000;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;a|s=9999|r=2|rg=1|1=Slimes become friendly|z=100000;|s=9999|rg=1|1=Charged with the essence of many souls;|s=9999|rg=1|1=Charged with the essence of many souls;|s=9999|rg=2|1={$CommonItemTooltip.RightClickToOpen}|z=5000;rc|s=9999|d=17|t=24|k=4.75|rg=99|z=25;a|s=9999|rg=1|1=Displays how many monsters have been killed|z=50000;a|s=9999|rg=1|1=Displays the phase of the moon|z=50000;da|s=9999|d=30|D=2|k=9|rg=1|1=Allows the player to dash into the enemy|2=Double tap a direction|z=100000;d|s=9999|r=8|d=120|t=15|k=8|tx=30|rg=1|1=Sparks emit from struck enemies|z=500000;a|s=9999|rg=1|1=Displays how fast the player is moving|z=50000;t|s=9999|rg=100;w|s=9999|rg=400;a|s=9999|rg=1|1=Displays the most valuable ore around you|z=50000;rb|s=9999|r=2|d=5|k=2|rg=1|z=50000;rb|s=9999|r=2|d=7|k=2|rg=1|z=50000;m|s=9999|r=8|d=52|t=45|k=4|m=30|rg=1|z=500000;d|s=9999|r=8|d=85|t=8|k=3.5|rg=1|1=Allows you to go into stealth mode|z=500000;r|s=9999|r=8|d=85|t=15|rg=1|z=500000;rb|s=9999|r=8|d=30|k=3|rg=99|z=100;4|s=9999|r=2|D=4|rg=1|1=Improves vision|z=50000;a|s=9999|r=8|rg=1|1=Turns the holder into a werewolf at night|2=Turns the holder into a merfolk when entering water|3=Minor increase to damage, melee speed,|4=critical strike chance, life regeneration,|5=defense, mining speed, and minion knockback|z=700000;|s=9999|rg=5|1=Bouncy and sweet!|z=15;c|s=9999|t=15|rg=100|1={$CommonItemTooltip.WorksWhenWet}|z=10;t|s=9999|rg=100|1=Very bouncy;t|s=9999|rg=100|z=80;c|s=9999|t=25|rg=99|1=A small explosion that will destroy most tiles|2=Very bouncy|z=400;rc|s=9999|d=65|t=40|k=8|rg=99|1=A small explosion that will not destroy tiles|2=Very bouncy|z=100;t|s=9999|rg=1|1=Makes surrounding creatures less hostile|z=500;a|s=9999|rg=1|1=Displays the name of rare creatures around you|z=50000;a|s=9999|rg=1|1=Displays your damage per second|z=50000;a|s=9999|rg=1|1=Displays fishing information|z=50000;a|s=9999|r=3|rg=1|1=Displays movement speed, damage per second, and valuable ore|z=150000;a|s=9999|r=3|rg=1|1=Displays number of monsters, kill count, and rare creatures|z=150000;a|s=9999|r=5|rg=1|1=Displays everything|z=250000;|s=9999|r=7|t=90|rg=1|1=Displays everything|2=Allows you to return home at will|z=400000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=200;|s=9999|r=4|t=18|rg=1|1=Used to catch critters and bait|2=Can catch lava critters too!|z=250000;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=200;|s=9999|rg=1|z=100;4|s=9999|D=5|rg=1|z=17500;5|s=9999|D=6|rg=1|z=14000;6|s=9999|D=5|rg=1|z=10500;|s=9999|r=2|rg=3|z=37500;t|s=9999|r=2|rg=5|z=10000;t|s=9999|rg=5|z=1250;t|s=9999|rg=5|z=2500;t|s=9999|r=2|rg=5|z=5000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Grubs up!|z=10000;c|s=9999|t=25|rg=99|1=A small explosion that will destroy most tiles|z=1000;rc|s=9999|d=17|t=13|k=3.5|rg=99|z=80;t|s=9999|rg=1|1={$CommonItemTooltip.UseableWhenPlaced}|2={$BuffDescription.Sharpened}|z=100000;|s=9999|t=90|rg=1|1=Gaze in the mirror to return home|z=50000;a|s=9999|rg=1|1=The wearer can run super fast|z=50000;a|s=9999|rg=1|1=Allows the holder to double jump|z=50000;t|s=9999|rg=1|z=500;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;m|s=9999|r=5|d=40|t=29|k=4.4|m=9|rg=1|1=Shoots an explosive crystal charge|z=200000;r|s=9999|r=5|d=43|k=3|rg=1|1=Spits toxic bubbles|z=200000;d|s=9999|r=5|d=55|t=28|k=5.75|rg=1|1=Spits an Ichor stream on contact|z=200000;a|s=9999|rg=1|1=Increases armor penetration by 5|z=50000;|s=9999|r=3|t=28|rg=1|1=Summons a flying piggy bank|2={$CommonItemTooltip.PersonalStorage}|z=100000;t|s=9999|rg=100|1=Blocks passing liquids|z=200;t|s=9999|rg=25|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=25|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=25|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=25|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=25|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=25|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=25|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=25|1={$CommonItemTooltip.Platform}|z=100;a|s=9999|rg=1|1=Has a chance to create illusions and dodge an attack|2=Temporarily increase critical chance and minion damage after dodge|3=May confuse nearby enemies after being struck|z=100000;a|s=9999|rg=1|1=Reduces damage taken by 17%|z=100000;a|s=9999|rg=1|1=Increases jump height|z=125000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Become the wind, ride the lightning.|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Bejeweled and elegant for soaring through the thundering skies|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|3=The Valkyrie Satellite Barrier Platform is totally safe. Most of the time.|z=400000;t|s=9999|rg=2;t|s=9999|rg=2;t|s=9999|rg=2;t|s=9999|rg=2;t|s=9999|rg=2;t|s=9999|rg=100;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;w|s=9999|rg=400;t|s=9999|rg=1;t|s=9999|rg=1;a|s=9999|rg=1|1=Increases jump height|2=Allows the holder to double jump|z=150000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;a|s=9999|r=2|rg=1|1=Shoots crossbones at enemies while you are attacking|z=100000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;s|s=9999|r=8|d=55|t=36|k=2|rg=1|1=Summons a deadly sphere to fight for you|z=500000;a|s=9999|r=4|rg=1|1=Allows the holder to double jump|2=Increases jump height and negates fall damage|z=150000;a|s=9999|r=4|rg=1|1=Releases bees and douses the user in honey when damaged|2=Increases jump height and negates fall damage|z=150000;a|s=9999|r=4|rg=1|1=Allows the holder to double jump|2=Increases jump height and negates fall damage|z=150000;t|s=9999|rg=1|z=20000;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;d|s=9999|r=4|c=15|d=55|t=20|k=20|rg=1|z=250000;c|s=9999|r=3|t=17|rg=1|z=300000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable unicorn mount|z=250000;t|s=9999|r=7|rg=25|z=50000;d|s=9999|r=2|d=21|t=25|k=3.25|rg=1|z=50000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;4|s=9999|D=4|rg=1|1=Increases summon damage by 8%|z=4500;5|s=9999|D=6|rg=1|1=Increases your max number of minions by 1|z=4500;6|s=9999|D=5|rg=1|1=Increases summon damage by 8%|z=4500;m|s=9999|r=4|d=40|t=20|k=2|m=15|rg=1|z=200000;t|s=9999|rg=1|1=Right Click to place item on item frame;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;d|s=9999|d=9|t=25|k=2.5|rg=1|z=500;d|s=9999|d=16|t=25|k=4.5|rg=1|z=50000;d|s=9999|d=17|t=25|k=4|rg=1|z=50000;d|s=9999|r=3|d=18|t=25|k=3.75|rg=1|z=65000;d|s=9999|r=3|d=27|t=25|k=4.3|rg=1|z=90000;d|s=9999|r=4|d=39|t=25|k=3.3|rg=1|z=200000;d|s=9999|r=5|d=54|t=25|k=3.8|rg=1|z=250000;d|s=9999|d=14|t=25|k=3.5|rg=1|z=25000;d|s=9999|r=7|d=60|t=25|k=3.1|rg=1|z=250000;d|s=9999|r=9|d=70|t=25|k=4.5|rg=1|1={$CommonItemTooltip.DevItem}|z=200000;d|s=9999|r=9|d=70|t=25|k=4.5|rg=1|1={$CommonItemTooltip.DevItem}|z=200000;d|s=9999|r=4|d=43|t=25|k=2.8|rg=1|z=200000;d|s=9999|r=4|d=39|t=25|k=4.5|rg=1|z=200000;d|s=9999|r=8|c=10|d=95|t=25|k=4.3|rg=1|z=550000;d|s=9999|r=8|d=115|t=25|k=3.5|rg=1|z=625000;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|rg=1|1={$CommonItemTooltip.String}|z=1500;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.Counterweight}|z=50000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.Counterweight}|z=50000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.Counterweight}|z=50000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.Counterweight}|z=50000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.Counterweight}|z=50000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.Counterweight}|z=50000;d|s=9999|r=3|d=39|t=25|k=3.25|rg=1|z=200000;d|s=9999|r=3|d=49|t=25|k=3.8|rg=1|z=200000;d|s=9999|r=3|d=28|t=25|k=3.85|rg=1|z=87500;c|s=9999|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=2|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=2|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=3|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=3|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=4|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=5|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=5|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=5|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=6|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=7|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=7|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=8|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=8|rg=3|1={$CommonItemTooltip.RightClickToOpen};a|s=9999|r=3|rg=1|1=Increases the strength of friendly bees|z=100000;a|s=9999|r=4|rg=1|1=Allows the use of two yoyos at once|z=500000;c|s=9999|r=4|t=30|rg=1|1=Permanently increases the number of accessory slots|z=100000;a|s=9999|r=8|rg=1|1=Summons spores over time that will damage enemies|z=200000;a|s=9999|r=8|rg=1|1=Greatly increases life regen when not moving|z=250000;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=200|1={$CommonItemTooltip.CanBeExtractinated};w|s=9999|rg=400;d|s=9999|r=2|d=20|t=18|k=4.25|rg=1|1=Rapid attacks deal more damage|z=25000;r|s=9999|r=2|c=7|d=12|t=24|k=1.25|rg=1|z=25000;d|s=9999|r=3|d=40|t=15|k=3.5|rg=1|1=Enables rapid tax collection|z=100000;d|s=9999|r=2|d=14|t=13|k=5|rg=1|z=25000;|s=9999|r=6|rg=1|z=50000;|s=9999|r=5|rg=1|z=25000;|s=9999|r=5|rg=1|z=25000;|s=9999|r=5|rg=1|z=25000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|1=Places living rich mahogany|z=12500;t|s=9999|rg=1|1=Places rich mahogany leaves|z=12500;5v|s=9999|rg=1|z=250000;6v|s=9999|rg=1|z=250000;t|s=9999|rg=1;t|s=9999|rg=1;a|s=9999|r=4|rg=1|1=Gives the user master yoyo skills|z=500000;|s=9999|r=8|t=20|rg=1|1=Attracts a legendary creature which flourishes in water & combat|z=250000;d|s=9999|r=9|d=25|t=25|k=4|rg=1|1=I didnt get this off of a Schmoo|z=250000;t|s=9999|r=3|rg=1|1=Right Click to adjust trajectory|2=Use a torch or wire signal to fire|z=250000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4|s=9999|D=4|rg=1|1=4% increased ranged critical strike chance|z=15000;5|s=9999|D=5|rg=1|1=5% increased ranged damage|z=25000;6|s=9999|D=4|rg=1|1=4% increased ranged critical strike chance|z=20000;m|s=9999|d=21|t=28|k=4.75|m=7|rg=1|z=20000;rc|s=9999|d=20|t=25|k=5|rg=99|z=50;rc|s=9999|d=14|t=14|k=1.5|rg=99|z=50;t|s=9999|rg=100;4|s=9999|r=10|D=10|rg=1|1=Increases your max number of minions by 1|2=Increases your max number of sentries by 1|3=Increases summon damage by 22%|z=350000;5|s=9999|r=10|D=16|rg=1|1=Increases your max number of minions by 2|2=Increases summon damage by 22%|3=Increases whip range by 15%|z=700000;6|s=9999|r=10|D=12|rg=1|1=Increases your max number of minions by 2|2=Increases summon damage by 22%|3=Increases whip range by 15%|z=525000;|s=9999|r=8|t=20|k=2|rg=1|1=Creates a pair of portals that can be traveled through.|2=Use Left Click & Right Click to place portals.|3=Increases momentum and falling speed.|4=Speedy thing goes in, speedy thing comes out.|z=500000;t|s=9999|rg=3|1=Can be traded for rare dyes|z=10000;t|s=9999|rg=3|1=Can be traded for rare dyes|z=10000;t|s=9999|rg=3|1=Can be traded for rare dyes|z=10000;t|s=9999|rg=3|1=Can be traded for rare dyes|z=10000;d|s=9999|r=10|c=10|d=190|t=25|k=6.5|rg=1|z=500000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GoblinSummoner}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Salamander}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GiantShelly}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Crawdad}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Fritz}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CreatureFromTheDeep}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DrManFly}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Mothron}|z=1000;t|s=9999|1={$CommonItemTooltip.BannerBonus}{$NPCName.None}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ThePossessed}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Butcher}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Psycho}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DeadlySphere}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Nailhead}|z=1000;t|s=9999|1={$CommonItemTooltip.BannerBonus}{$NPCName.None}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Medusa}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GreekSkeleton}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GraniteFlyer}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GraniteGolem}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BloodZombie}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Drippler}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.TombCrawlerHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DuneSplicerHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.FlyingAntlion}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.WalkingAntlion}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DesertGhoul}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DesertLamiaDark}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DesertDjinn}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DesertBeast}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DesertScorpionWalk}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.StardustSoldier}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.StardustWormHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.StardustJellyfishBig}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.StardustSpiderBig}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.StardustCellSmall}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.StardustCellBig}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SolarCorite}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SolarSroller}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SolarCrawltipedeHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SolarDrakomireRider}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SolarDrakomire}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SolarSolenian}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.NebulaSoldier}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.NebulaHeadcrab}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.NebulaBrain}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.NebulaBeast}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.VortexLarva}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.VortexHornetQueen}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.VortexHornet}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.VortexSoldier}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.VortexRifleman}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PirateCaptain}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PirateDeadeye}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PirateCorsair}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PirateCrossbower}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MartianWalker}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.RedDevil}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.PinkJellyfish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GreenJellyfish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.DarkMummy}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.LightMummy}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.AngryBones}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.IceTortoise}|z=1000;1;1;1;|s=9999|r=9|rg=25|1=Swirling energies emanate from this fragment|z=10000;|s=9999|r=9|rg=25|1=The power of a galaxy resides within this fragment|z=10000;|s=9999|r=9|rg=25|1=The fury of the universe lies within this fragment|z=10000;|s=9999|r=9|rg=25|1=Entrancing particles revolve around this fragment|z=10000;t|s=9999|r=10|rg=100|1=A pebble of the heavens|z=15000;t|s=9999|r=9|rg=100;d|s=9999|r=10|d=100|t=25|k=6|tx=27|tr=4|z=300000;d|s=9999|r=10|d=80|t=15|k=4|tx=27|tr=3|z=300000;d|s=9999|r=10|d=50|t=15|k=0.5|tp=225|tr=2|rg=1|z=350000;d|s=9999|r=10|d=110|t=30|k=7|th=100|tr=4|z=400000;d|s=9999|r=10|d=80|t=12|k=5.5|tp=225|tr=4|rg=1|z=350000;t|s=9999|r=10|rg=25|1=It vibrates with luminous celestial energy|z=60000;a|s=9999|r=10|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;a|s=9999|r=10|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|2={$CommonItemTooltip.PressDownToHover}|z=400000;a|s=9999|r=10|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|2={$CommonItemTooltip.PressDownToHover}|z=400000;a|s=9999|r=10|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=400000;w|s=9999|r=9|rg=400;d|s=9999|r=10|d=105|t=20|k=2|rg=1|1=Strike with the fury of the sun|z=500000;s|s=9999|r=10|d=60|t=36|k=2|rg=1|1=Summons a stardust cell to fight for you|2=Cultivate the most beautiful cellular infection|z=500000;r|s=9999|r=10|d=50|t=20|k=2|rg=1|1=66% chance to save ammo|2=The catastrophic mixture of pew pew and boom boom.|z=500000;m|s=9999|r=10|d=70|t=30|k=5|m=30|rg=1|1=Conjure masses of astral energy to chase down your foes|z=500000;c|s=9999|r=3|d=20|t=15|k=3|rg=99|1=Spreads the Crimson to some blocks|z=100;4v|s=9999|rg=1|1=Wuv... twue wuv...|z=5000;5v|s=9999|rg=1|1=Mawwiage...|z=5000;r|s=9999|d=13|t=25|rg=1|z=10500;d|s=9999|d=10|t=27|k=5.5|th=59|rg=1|z=12000;d|s=9999|d=8|t=25|k=4.5|tx=12|rg=1|z=12000;d|s=9999|d=13|k=5|rg=1|z=10500;d|s=9999|d=16|t=17|k=6.5|rg=1|z=13500;d|s=9999|d=7|t=19|k=2|tp=59|rg=1|1=Can mine Meteorite|z=15000;r|s=9999|d=10|t=26|rg=1|z=5250;d|s=9999|d=9|t=28|k=5.5|th=50|rg=1|z=6000;d|s=9999|d=7|t=26|k=4.5|tx=11|rg=1|z=6000;d|s=9999|d=10|t=11|k=4|rg=1|z=5250;d|s=9999|d=14|t=19|k=6|rg=1|z=6750;d|s=9999|d=6|t=21|k=2|tp=50|rg=1|1=Can mine Meteorite|z=7500;r|s=9999|d=9|t=27|rg=1|z=2100;d|s=9999|d=8|t=29|k=5.5|th=43|rg=1|z=2400;d|s=9999|d=6|t=28|k=4.5|tx=10|rg=1|z=2400;d|s=9999|d=9|t=12|k=4|rg=1|z=2100;d|s=9999|d=13|t=20|k=5.5|rg=1|z=2700;d|s=9999|d=6|t=19|k=2|tp=43|rg=1|z=3000;r|s=9999|d=7|t=28|rg=1|z=525;d|s=9999|d=6|t=31|k=5.5|th=38|rg=1|z=600;d|s=9999|d=4|t=28|k=4.5|tx=8|rg=1|z=600;d|s=9999|d=7|t=12|k=4|rg=1|z=525;d|s=9999|d=10|t=20|k=5.5|rg=1|z=675;d|s=9999|d=5|t=21|k=2|tp=35|rg=1|z=750;r|s=9999|d=6|t=29|rg=1|z=350;d|s=9999|d=4|t=33|k=5.5|th=35|rg=1|z=400;d|s=9999|d=3|t=30|k=4.5|tx=7|rg=1|z=400;d|s=9999|d=5|t=13|k=4|rg=1|z=350;d|s=9999|d=9|t=21|k=5.5|rg=1|z=450;d|s=9999|d=4|t=23|k=2|tp=35|rg=1|z=500;r|s=9999|d=9|t=27|rg=1|z=3500;d|s=9999|d=9|t=29|k=5.5|th=45|rg=1|z=4000;d|s=9999|d=6|t=26|k=4.5|tx=10|rg=1|z=4000;d|s=9999|d=9|t=12|k=4|rg=1|z=3500;d|s=9999|d=14|t=20|k=6|rg=1|z=4500;d|s=9999|d=6|t=19|k=2|tp=45|rg=1|z=5000;r|s=9999|d=11|t=26|rg=1|z=7000;d|s=9999|d=9|t=28|k=5.5|th=55|rg=1|z=8000;d|s=9999|d=7|t=26|k=4.5|tx=11|rg=1|z=8000;d|s=9999|d=12|t=11|k=5|rg=1|z=7000;d|s=9999|d=15|t=18|k=6.5|rg=1|z=9000;d|s=9999|d=6|t=20|k=2|tp=55|rg=1|1=Can mine Meteorite|z=10000;d|s=9999|r=10|d=60|t=28|k=7|tx=30|th=100|tr=4|rg=1|z=250000;d|s=9999|r=10|d=60|t=28|k=7|tx=30|th=100|tr=4|rg=1|z=250000;d|s=9999|r=10|d=60|t=28|k=7|tx=30|th=100|tr=4|rg=1|z=250000;d|s=9999|r=10|d=60|t=28|k=7|tx=30|th=100|tr=4|rg=1|z=250000;|s=9999|r=4|rg=3|z=125000;|s=9999|r=4|rg=3|z=125000;|s=9999|r=4|rg=3|z=125000;|s=9999|r=4|rg=3|z=125000;|s=9999|r=4|rg=3|z=125000;s|s=9999|r=10|d=40|t=36|k=2|rg=1|1=Summons a stardust dragon to fight for you|2=Who needs a horde of minions when you have a giant dragon?|z=500000;c|s=9999|r=4|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=Bacon? Bacon.|z=50000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=2|rg=3|z=37500;|s=9999|r=3|rg=3|z=75000;avt|s=9999|r=9|rg=1|1=Wield a small amount of power from the Vortex Tower|z=1000000;avt|s=9999|r=9|rg=1|1=Wield a small amount of power from the Nebula Tower|z=1000000;avt|s=9999|r=9|rg=1|1=Wield a small amount of power from the Stardust Tower|z=1000000;avt|s=9999|r=9|rg=1|1=Wield a small amount of power from the Solar Tower|z=1000000;r|s=9999|r=10|d=50|t=12|k=2|rg=1|1=66% chance to save ammo|z=500000;m|s=9999|r=10|d=100|k=0.25|m=12|rg=1|1=Fire a lifeform disintegration rainbow|z=500000;m|s=9999|r=10|d=130|t=12|k=3|m=12|rg=1|1=From Orions belt to the palm of your hand|z=500000;d|s=9999|r=10|d=150|t=16|k=5|rg=1|1=Rend your foes asunder with a spear of light!|z=500000;c|s=9999|r=7|hl=200|t=17|rg=30|z=15000;t|s=9999|rg=1|1={$CommonItemTooltip.WireTrigger}|z=10000;r|s=9999|r=8|d=25|t=30|k=4|rg=1|z=800000;c|s=9999|t=40|rg=99|1=A large explosion that will destroy most tiles|2=This will prove to be a terrible idea|z=2000;rc|s=9999|r=2|d=30|t=20|k=6|rg=99|1=A small explosion that will not destroy tiles|z=250;t|s=9999|r=10|rg=1|1=Used to craft items from Lunar Fragments and Luminite|z=250000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|r=2|rg=3|z=37500;|s=9999|r=2|rg=3|z=37500;|s=9999|r=2|rg=3|z=37500;|s=9999|r=3|rg=3|z=75000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|rg=3|z=10000;|s=9999|r=2|rg=3|z=10000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=3|rg=3|z=75000;t|s=9999|rg=5|z=2500;t|s=9999|r=3|rg=3|z=500000;t|s=9999|rg=1;t|s=9999|r=3|rg=1|z=500000;rb|s=9999|r=9|d=20|k=3|rg=99|1=Line em up and knock em down...|z=10;rb|s=9999|r=9|d=15|k=3.5|rg=99|1=Shooting them down at the speed of sound!|z=10;sS|s=9999|r=10|d=100|t=30|k=7.5|rg=1|1={$CommonItemTooltip.Sentry}|2=Summons a lunar portal to shoot lasers at your enemies|z=500000;m|s=9999|r=10|d=100|k=4.5|m=9|rg=1|1=Rains down lunar flares|z=500000;sS|s=9999|r=10|d=130|t=30|k=7.5|rg=1|1={$CommonItemTooltip.Sentry}|2=Summons a radiant crystal that banishes your enemies|3=The colors, Duke, the colors!|z=500000;|s=9999|r=10|t=20|rg=1|1=You want the moon? Just grapple it and pull it down!|2={$CommonItemTooltip.Hook}|z=500000;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;|s=9999|r=10|t=20|rg=1|1=Calls upon a suspicious looking eye to provide light|2=I know what youre thinking....|z=500000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|3=Whatever this accessory does to you is not a bug!|z=400000;av|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=If you see this you should probably run away...|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;w|s=9999|rg=400;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Disorder came from order, fear came from courage, weakness came from strength|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Know thy self, know thy enemy. A thousand battles, a thousand victories\u2026|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Wheels of justice grind slow but grind fine.|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|3=Let your plans be dark and impenetrable as night, and when you move, fall like a thunderbolt.|z=400000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SandSlime}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SeaSnail}|z=1000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=30000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=9|rg=3|1={$CommonItemTooltip.DevItem}|z=150000;|s=9999|r=3|rg=3|z=75000;c|s=9999|r=10|t=45|rg=3|1=Summons the Impending Doom;t|s=9999|rg=5|1=Place this on logic gates to add checks|2=Turns on when signalled|z=1000;t|s=9999|rg=5|1={$CommonItemTooltip.LogicGate}|2="All logic gate lamps above are on"|z=20000;t|s=9999|rg=5|1={$CommonItemTooltip.LogicGate}|2="Any logic gate lamp above is on"|z=20000;t|s=9999|rg=5|1={$CommonItemTooltip.LogicGate}|2="Not all logic gate lamps above are on"|z=20000;t|s=9999|rg=5|1={$CommonItemTooltip.LogicGate}|2="No logic gate lamps above are on"|z=20000;t|s=9999|rg=5|1={$CommonItemTooltip.LogicGate}|2="Exactly one logic gate lamps above is on"|z=20000;t|s=9999|rg=5|1={$CommonItemTooltip.LogicGate}|2="Zero or more than one logic gate lamps above are on"|3=Also known as NXOR|z=20000;t|s=9999|rg=100|1=Switches direction when signalled|z=500;t|s=9999|rg=100|1=Switches direction when signalled|z=500;|s=9999|r=2|rg=1|1=Allows ultimate control over wires!|2=Right Click while holding to edit wire settings|z=200000;|s=9999|t=15|tr=20|rg=1|1=Places yellow wire|z=20000;t|s=9999|rg=5|1=Signals at dawn;t|s=9999|rg=5|1=Signals at dusk;t|s=9999|rg=5|1=Signals when the first player enters or the last player leaves the area above it;t|s=9999|rg=25|1=Separates wire paths|2=Hammer to change direction|z=200;t|s=9999|rg=1|1=Sends a message to everyone when signalled;t|s=9999|rg=5|1=Place this on logic gates to add checks|2=Turns off when signalled|z=1000;a|s=9999|r=3|rg=1|1=Grants improved wire vision|z=10000;|s=9999|t=15|tr=20|rg=1|1=Activates Actuators|z=20000;t|s=9999|rg=100|z=100;t|s=9999|rg=200|1={$CommonItemTooltip.Platform}|z=100;|s=9999|r=10|t=20|rg=1|1={$CommonItemTooltip.Hook}|z=500000;a|s=9999|r=3|rg=1|1=Automatically places actuators on placed objects|z=100000;|s=9999|t=15|tr=20|rg=1|1=Right Click while holding to edit wire settings|z=120000;t|s=9999|rg=5|1=Signals when stepped on or off by a player;4v|s=9999|rg=1|z=10000;|s=9999|t=20|rg=1|1=Susceptible to lava!|z=5000000;t|s=9999|r=2|rg=5|1=Lights up bulbs for each wire color|z=50000;t|s=9999|rg=5|1=Signals when stepped on or off by a player;t|s=9999|rg=5|1=Signals when stepped on or off by a player;t|s=9999|rg=5|1=Signals when stepped on or off by a player;t|s=9999|rg=100|z=100;t|s=9999|rg=100|z=100;t|s=9999|rg=100|z=100;t|s=9999|rg=100|z=100;t|s=9999|rg=100|z=100;t|s=9999|rg=200|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=200|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=200|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=200|1={$CommonItemTooltip.Platform}|z=100;t|s=9999|rg=200|1={$CommonItemTooltip.Platform}|z=100;|s=9999|rg=1|1=For Capture the Gem. It drops when you die;t|s=9999|rg=1|1=Right Click to place or remove Large Rubies|z=500;t|s=9999|rg=1|1=Right Click to place or remove Large Sapphires|z=500;t|s=9999|rg=1|1=Right Click to place or remove Large Emeralds|z=500;t|s=9999|rg=1|1=Right Click to place or remove Large Topazes|z=500;t|s=9999|rg=1|1=Right Click to place or remove Large Amethysts|z=500;t|s=9999|rg=1|1=Right Click to place or remove Large Diamonds|z=500;t|s=9999|rg=1|1=Right Click to place or remove Large Ambers|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=5|1=Place this on logic gate lamps|2=When signalled, randomly chooses a lamp below it. If that lamp is on, the logic gate signals|z=20000;t|s=9999|r=3|rg=1|1=Right Click to adjust trajectory, change portal colour and fire|z=100000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|z=500;t|s=9999|z=500;t|s=9999|rg=5|1=Signals when a projectile touches it|z=20000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;a|s=9999|r=3|rg=1|1=Fishing line will never break, decreases chance of bait consumption, increases fishing power by 10|z=150000;t|s=9999|rg=5|1=Activates when touched or signalled|z=10000;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=25|1=Separates wire paths|2=Toggle lights with simultaneous crossed signals|z=200;t|s=9999|rg=5|1=Signals when water starts or stops touching it;t|s=9999|rg=5|1=Signals when lava starts or stops touching it;t|s=9999|rg=5|1=Signals when honey starts or stops touching it;t|s=9999|rg=5|1=Signals when any liquid starts or stops touching it;av|s=9999|rg=1|z=20000;av|s=9999|rg=1|z=20000;4v|s=9999|rg=1|z=10000;4v|s=9999|rg=1|z=30000;5v|s=9999|rg=1|z=30000;6v|s=9999|rg=1|z=30000;t|s=9999|rg=100|1=Smells like bubblegum and happiness|z=100;t|s=9999|rg=100|1=Smells like lavender and enthusiasm|z=100;t|s=9999|rg=100|1=Smells like mint and glee|z=100;t|s=9999|tr=3|rg=100|1=Oddly durable enough to climb!|z=50;t|s=9999|tr=3|rg=100|1=Oddly durable enough to climb!|z=50;t|s=9999|tr=3|rg=100|1=Oddly durable enough to climb!|z=50;t|s=9999|rg=1|1=It never stops celebrating!|z=50000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|1=Beat the shindig out of it!|2=May contain a suprise!|z=10000;t|s=9999|r=3|rg=1|1=Balloons shall rain from the sky|z=200000;t|s=9999|rg=1|1=Tied down for everyones pleasure|z=2000;t|s=9999|rg=1|1={$CommonItemTooltip.PlacementStyle}|2=Wonder whats inside?|z=2000;t|s=9999|r=3|rg=3|1={$CommonItemTooltip.UseableWhenPlaced}|2=Stuff your face. Stuff someone elses face. Whatever.|z=25000;w|s=9999|rg=400|1=Productivity up 200%;w|s=9999|rg=400|1=Falling sand you can safely watch;w|s=9999|rg=400|1=A lot cooler than a snow globe;t|s=9999|rg=100|1=Falling sand you can safely watch|z=5;t|s=9999|rg=100|1=A lot cooler than a snow globe;t|s=9999|rg=100|1={$CommonItemTooltip.PreventFallDamage};4v|s=9999|r=9|rg=1|1=Become the Pedguin|2=Great for impersonating streamers!|z=15000;5v|s=9999|r=9|rg=1|1=Become the Pedguin|2=Great for impersonating streamers!|z=15000;6v|s=9999|r=9|rg=1|1=Become the Pedguin|2=Great for impersonating streamers!|z=15000;w|s=9999|rg=400|1=Smells like bubblegum and happiness;w|s=9999|rg=400|1=Smells like lavender and enthusiasm;w|s=9999|rg=400|1=Smells like mint and glee;4v|s=9999|r=9|rg=1|1=Enables your inner wingman|2=Great for impersonating streamers!|z=50000;d|s=9999|r=4|d=50|t=16|k=3|rg=1|z=50000;d|s=9999|r=4|d=50|t=16|k=3|rg=1|z=50000;d|s=9999|r=4|d=50|t=16|k=3|rg=1|z=50000;d|s=9999|r=4|d=50|t=16|k=3|rg=1|z=50000;d|s=9999|r=4|d=50|t=16|k=3|rg=1|z=50000;d|s=9999|r=4|d=50|t=16|k=3|rg=1|z=50000;6|s=9999|r=4|rg=1|1=Grants slow fall in exchange for your feet|z=50000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable basilisk mount|z=250000;d|s=9999|r=2|c=7|d=16|t=15|k=4.5|rg=1|z=5000;4v|s=9999|r=4|rg=1|z=25000;5v|s=9999|r=4|rg=1|z=25000;6v|s=9999|r=4|rg=1|z=25000;4|s=9999|r=5|D=6|rg=1|1=15% increased magic and summon damage|z=250000;5|s=9999|r=5|D=12|rg=1|1=Increases maximum mana by 40|2=10% increased summon damage|3=Increases your max number of minions by 1|z=200000;6|s=9999|r=5|D=8|rg=1|1=Increases maximum mana by 40|2=10% increased magic damage|3=Increases your max number of minions by 1|z=150000;m|s=9999|r=4|d=85|t=22|k=5|m=14|rg=1|z=50000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SandElemental}|z=1000;a|s=9999|r=3|rg=1|1=Grants immunity to Stoned|z=100000;t|s=9999|rg=1|z=200;|s=9999|r=5|rg=3|z=50000;6v|s=9999|r=3|rg=1|z=25000;5v|s=9999|r=3|rg=1|z=25000;4v|s=9999|r=3|rg=1|z=25000;m|s=9999|r=4|c=20|d=38|t=12|k=6|m=17|rg=1|z=300000;r|s=9999|r=4|d=24|t=48|k=6.5|rg=1|z=250000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SandShark}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SandsharkCorrupt}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SandsharkCrimson}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SandsharkHallow}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Tumbleweed}|z=1000;|s=9999|r=3|rg=5|z=5000;t|s=9999|r=3|rg=1|z=25000;avt|s=9999|r=4|rg=1|z=100000;4|s=9999|r=8|D=7|rg=1|1=Increases your max number of sentries by 1, 10% increased magic damage and 10% reduced mana cost|z=150000;5|s=9999|r=8|D=15|rg=1|1=20% increased summon damage and 10% increased magic damage|z=150000;6|s=9999|r=8|D=10|rg=1|1=10% increased summon damage, 20% increased magic critical strike chance and movement speed|z=150000;4|s=9999|r=8|D=13|rg=1|1=Increases your max number of sentries by 1 and increases your life regeneration|z=150000;5|s=9999|r=8|D=27|rg=1|1=15% increased summon and melee damage|z=150000;6|s=9999|r=8|D=18|rg=1|1=15% increased summon damage, 15% increased melee critical strike chance and movement speed|z=150000;4|s=9999|r=8|D=7|rg=1|1=Increases your max number of sentries by 1 and increases ranged critical strike chance by 10%|z=150000;5|s=9999|r=8|D=17|rg=1|1=20% increased summon and ranged damage and 10% chance to save ammo|z=150000;6|s=9999|r=8|D=12|rg=1|1=10% increased summon damage and 20% increased movement speed|z=150000;4|s=9999|r=8|D=8|rg=1|1=Increases your max number of sentries by 1 and increases melee speed by 20%|z=150000;5|s=9999|r=8|D=22|rg=1|1=20% increased summon and melee damage|z=150000;6|s=9999|r=8|D=16|rg=1|1=10% increased summon damage,|2=15% increased melee critical strike chance and 20% increased movement speed|z=150000;a|s=9999|r=5|rg=1|1={$ItemTooltip.HuntressBuckler}|z=150000;a|s=9999|r=5|rg=1|1={$ItemTooltip.HuntressBuckler}|z=150000;a|s=9999|r=5|rg=1|1=Increase your max number of sentries by 1|2=Increases summon damage by 10%|z=150000;a|s=9999|r=5|rg=1|1={$ItemTooltip.HuntressBuckler}|z=150000;t|s=9999|r=3|rg=1|1={$CommonItemTooltip.PersonalStorage}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.UseableWhenPlaced}|2={$BuffDescription.WarTable}|z=100000;t|s=9999|rg=1|z=100000;t|s=9999|r=3|rg=1|1=Holds the Eternia Crystal|2=Interact while carrying an Eternia Crystal to summon Etherias portals|3=Interact with the crystal to skip extra time between waves|z=10000;|s=9999|r=3|rg=50|1=Currency for trading with the Tavernkeep;sS|s=9999|r=3|d=17|t=30|k=3|rg=1|1={$CommonItemTooltip.Sentry}|2=An average speed tower that shoots exploding fireballs|3={$CommonItemTooltip.EtherianManaCost10}|z=50000;sS|s=9999|r=5|d=42|t=30|k=3|rg=1|1={$ItemTooltip.DD2FlameburstTowerT1Popper}|z=250000;sS|s=9999|r=8|d=88|t=30|k=3|rg=1|1={$ItemTooltip.DD2FlameburstTowerT1Popper}|z=750000;d|s=9999|r=2|d=32|t=32|k=4|rg=1|1=69% chance to save ammo|z=12500;|s=9999|1=Often used to manifest ones will as a physical form of defense;d|s=9999|r=5|d=95|t=20|k=6.5|rg=1|1=Right Click to guard with a shield|z=50000;sS|s=9999|r=3|d=30|t=30|k=4.7|rg=1|1={$CommonItemTooltip.Sentry}|2=A slow but high damage tower that shoots piercing bolts|3={$CommonItemTooltip.EtherianManaCost10}|z=50000;sS|s=9999|r=5|d=74|t=30|k=4.7|rg=1|1={$ItemTooltip.DD2BallistraTowerT1Popper}|z=250000;sS|s=9999|r=8|d=156|t=30|k=4.7|rg=1|1={$ItemTooltip.DD2BallistraTowerT1Popper}|z=750000;d|s=9999|r=8|d=180|t=20|k=5.5|rg=1|1=Unleashes the hearts energy forward|z=250000;c|s=9999|r=3|rg=3|1=Place in the Eternia Crystal Stand to summon Etherias portals|z=2500;sS|s=9999|r=3|d=4|t=30|k=0.25|rg=1|1={$CommonItemTooltip.Sentry}|2=An aura that repeatedly zaps enemies that go inside|3=Aura damage penetrates enemy defense|4=Hits quickly at the expense of 50% tag damage|5={$CommonItemTooltip.EtherianManaCost10}|z=50000;sS|s=9999|r=5|d=11|t=30|k=0.25|rg=1|1={$ItemTooltip.DD2LightningAuraT1Popper}|z=250000;sS|s=9999|r=8|d=34|t=30|k=0.25|rg=1|1={$ItemTooltip.DD2LightningAuraT1Popper}|z=750000;sS|s=9999|r=3|d=24|t=30|k=0.5|rg=1|1={$CommonItemTooltip.Sentry}|2=A trap that explodes when enemies come near|3={$CommonItemTooltip.EtherianManaCost10}|z=50000;sS|s=9999|r=5|d=59|t=30|k=0.5|rg=1|1={$ItemTooltip.DD2ExplosiveTrapT1Popper}|z=250000;sS|s=9999|r=8|d=126|t=30|k=0.5|rg=1|1={$ItemTooltip.DD2ExplosiveTrapT1Popper}|z=750000;d|s=9999|r=5|d=50|t=30|k=7|rg=1|1=Charges power as it is swung to smash enemies|z=50000;d|s=9999|r=5|d=45|t=27|k=7|rg=1|1=Summons ghosts as it hits enemies|z=50000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2GoblinBomberT1}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2GoblinT1}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2SkeletonT1}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2DrakinT2}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2KoboldFlyerT2}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2KoboldWalkerT2}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2WitherBeastT2}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2WyvernT1}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2JavelinstT1}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonusReduced}{$NPCName.DD2LightningBugT3}|z=1000;|s=9999;|s=9999;|s=9999;|s=9999;|s=9999;m|s=9999|r=5|d=36|t=25|k=9|m=20|rg=1|1=Gotta wonder who stuck a tome of infinite wisdom on a stick...|2=Right Click to cast a powerful tornado|z=50000;|s=9999;r|s=9999|r=5|d=32|t=18|k=2|rg=1|1=Harnesses the power of undying flames|z=50000;|s=9999|r=3|t=20|rg=1|1=Summons a pet gato|z=100000;|s=9999|r=3|t=20|rg=1|1=Summons a pet flickerwick to provide light|z=100000;|s=9999|r=3|t=20|rg=1|1=Summons a pet dragon|z=100000;d|s=9999|r=8|d=140|t=30|k=5|rg=1|1=Right Click while holding for an alternate attack!|z=250000;r|s=9999|r=8|c=3|d=38|t=30|k=4.5|rg=1|1=Shoots splitting arrows, deals more damage to airborne enemies|z=250000;c|s=9999|r=8|rg=3|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=5|1={$CommonItemTooltip.RightClickToOpen};c|s=9999|r=3|1={$CommonItemTooltip.RightClickToOpen};4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;4v|s=9999|rg=1|z=37500;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;avt|s=9999|r=4|rg=1|z=100000;m|s=9999|r=8|d=100|t=20|k=7|m=14|rg=1|1=Splashes defense reducing miasma!|z=250000;4|s=9999|r=8|D=20|rg=1|1=Increases your max number of sentries by 2 and increases summon and melee damage by 10%|z=150000;5|s=9999|r=8|D=24|rg=1|1=30% increased summon damage and massively increased life regeneration|z=150000;6|s=9999|r=8|D=24|rg=1|1=20% increased summon damage and melee critical strike chance|2=20% increased movement speed|z=150000;4|s=9999|r=8|D=7|rg=1|1=Increases your max number of sentries by 2|2=15% increased summon & magic damage|z=150000;5|s=9999|r=8|D=21|rg=1|1=25% increased summon damage and 10% increased magic damage|2=15% reduced mana cost|z=150000;6|s=9999|r=8|D=14|rg=1|1=20% increased summon damage and 25% increased magic critical strike chance|2=20% increased movement speed|z=150000;4|s=9999|r=8|D=8|rg=1|1=Increases your max number of sentries by 2|2=10% increased summon damage and ranged critical strike chance|z=150000;5|s=9999|r=8|D=24|rg=1|1=25% increased summon & ranged damage and 20% chance to save ammo|z=150000;6|s=9999|r=8|D=16|rg=1|1=25% increased summon damage and 10% increased ranged critical strike chance|2=20% increased movement speed|z=150000;4|s=9999|r=8|D=10|rg=1|1=Increases your max number of sentries by 2 and increases summon & melee damage by 20%|z=150000;5|s=9999|r=8|D=26|rg=1|1=20% increased summon damage and melee speed|2=5% increased melee critical strike chance|z=150000;6|s=9999|r=8|D=18|rg=1|1=20% increased summon damage and melee critical strike chance|2=30% increased movement speed|z=150000;a|s=9999|r=8|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|2={$CommonItemTooltip.PressDownToHover}|z=400000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=I didnt get this from the Grid|z=250000;5v|s=9999|r=9|rg=1|1={$ItemTooltip.ArkhalisHat}|z=250000;6v|s=9999|r=9|rg=1|1={$ItemTooltip.ArkhalisHat}|z=250000;a|s=9999|r=9|rg=1|1={$ItemTooltip.ArkhalisHat}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=To keep those luscious locks as gorgeous as ever|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Bringing sexy back|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=There might be pasta in the pockets|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|3=Its full on! What does it mean?!|z=400000;av|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Brought to you by LeinCorp|z=250000;r|s=9999|r=10|d=50|t=6|k=10|rg=1|1=50% chance to save ammo|2=All good things end with a bang... or many!|z=500000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|1=Spider-sink, Spider-sink, does whatever a spider can...|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|1=Right Click when placed to display hats and helmets;|s=9999|r=3|z=75000;t|s=9999|rg=10|1={$ItemTooltip.WoodenCrate}|z=5000;t|s=9999|r=2|rg=10|1={$ItemTooltip.IronCrate}|z=25000;t|s=9999|r=3|rg=10|1={$ItemTooltip.GoldenCrate}|z=100000;t|s=9999|r=2|rg=5|1={$ItemTooltip.CorruptFishingCrate}|z=50000;t|s=9999|r=2|rg=5|1={$ItemTooltip.CrimsonFishingCrate}|z=50000;t|s=9999|r=2|rg=5|1={$ItemTooltip.DungeonFishingCrate}|z=50000;t|s=9999|r=2|rg=5|1={$ItemTooltip.FloatingIslandFishingCrate}|z=50000;t|s=9999|r=2|rg=5|1={$ItemTooltip.HallowedFishingCrate}|z=50000;t|s=9999|r=2|rg=5|1={$ItemTooltip.JungleFishingCrate}|z=50000;t|s=9999|rg=1|1=Signals when opened|z=500;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=5|rg=1|1=The wearer can run super fast|2=Increases jump speed and allows auto-jump|3=Increases fall resistance|z=100000;a|s=9999|r=5|rg=1|1=8% reduced mana cost|2=Automatically use mana potions when needed|3=Enemies are less likely to target you|z=500000;a|s=9999|r=5|D=8|rg=1|1=Increases melee knockback|2=12% increased melee speed|3=Enables auto swing for melee weapons|4=Increases the size of melee weapons|5=Enemies are more likely to target you|z=500000;a|s=9999|r=5|rg=1|1=Allows flight|2=The wearer can run super fast|3=Flowers grow on the grass you walk on|z=400000;a|s=9999|r=5|rg=1|1=Grants the ability to swim|2=Increases jump speed and allows auto-jump|3=Increases fall resistance|z=100000;a|s=9999|r=5|rg=1|1=Grants the ability to swim|2=Allows the ability to climb walls|3=Increases jump speed and allows auto-jump|4=Increases fall resistance|5=It aint easy being green|z=250000;a|s=9999|r=5|rg=1|1=Allows the ability to climb walls|2=Increases jump speed and allows auto-jump|3=Increases fall resistance|z=100000;a|s=9999|r=5|D=6|rg=1|1=Grants immunity to knockback|2=Puts a shell around the owner when below 50% life that reduces damage by 25%|3=Absorbs 25% of damage done to players on your team when above 25% life|z=400000;a|s=9999|r=5|D=10|rg=1|1=Grants immunity to knockback|2=Absorbs 25% of damage done to players on your team when above 25% life|3=Enemies are more likely to target you|z=500000;a|s=9999|r=5|rg=1|1=Provides 7 seconds of immunity to lava|2=Grants immunity to fire blocks|z=375000;a|s=9999|r=5|rg=1|1=8% reduced mana cost|2=Automatically use mana potions when needed|3=Increases pickup range for mana stars|z=200000;a|s=9999|r=5|rg=1|1=8% reduced mana cost|2=Automatically use mana potions when needed|3=Causes stars to fall after taking damage|4=Stars restore mana when collected|z=150000;a|s=9999|r=5|rg=1|1=Increases arrow damage by 10% and greatly increases arrow speed|2=20% chance not to consume arrows|3=Lights wooden arrows ablaze|4=Quiver in fear!|z=375000;a|s=9999|r=6|rg=1|1=Provides 7 seconds of immunity to lava|2=Grants immunity to fire blocks|3=Reduces damage from touching lava|z=450000;a|s=9999|r=5|rg=1|1=Grants immunity to fire blocks|2=Reduces damage from touching lava|z=150000;a|s=9999|r=5|rg=1|1=Increases view range for guns (Right Click to zoom out)|2=10% increased ranged damage and critical strike chance|3=Enemies are less likely to target you|4=Enemy spotted|z=500000;a|s=9999|r=5|rg=1|1=Increases arrow damage by 10% and greatly increases arrow speed|2=20% chance not to consume arrows|3=Enemies are less likely to target you|z=500000;a|s=9999|r=5|rg=1|1=Increases armor penetration by 5|2=Releases bees and douses the user in honey when damaged|z=150000;4|s=9999|r=5|D=4|rg=1|1=Improves vision and provides light when worn|2=The darkness holds no secrets for you|z=100000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=An apple a day keeps Doctor Bones away!|z=10000;|s=9999|1={$CommonItemTooltip.MediumStats}|2=Mmmm... pie.;c|s=9999|r=4|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=Nothing is as Terrarian as apple pie.|z=30000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Make like a banana and split!|z=10000;c|s=9999|r=4|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=Grilled to perfection!|z=50000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=This ones luck has run out.|z=10000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=...but wait! It was 99 cents.|z=20000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Caution: may contain harpy.|z=20000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Fresh from the oven|z=30000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Its so fizzy!|z=20000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Look at that S Car Go!|z=10000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Sunny side up!|z=20000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Wheres the Ketchup!?|z=10000;c|s=9999|r=6|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=(Au)some!|z=500000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Wrath not included.|z=20000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Thatll keep you out of my birdfeeders...|z=10000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=Hot diggity!|z=30000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Eat it before it melts!|z=20000;c|s=9999|r=4|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=It brings the boys to the yard!|z=30000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Its nach-yos, its mine!|z=20000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=With pepperoni and extra cheese.|z=20000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Betcha cant eat just one!|z=15000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Serving Size: 1 child.|z=10000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Better than chicken from a wall.|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=The other, other white meat.|z=5000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=I sea food, I eat it.|z=5000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Po Boy are you in for a treat!|z=25000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=Al dente!|z=20000;c|s=9999|r=4|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=Well done with ketchup!|z=30000;a|s=9999|r=5|rg=1|1=Provides 7 seconds of immunity to lava|2=Grants immunity to fire blocks|z=375000;|s=9999|r=2|t=12|rg=1|1={$CommonItemTooltip.GolfIron}|z=10000;t|s=9999|r=2|rg=1|1=Aim to sink your golf ball in the cup|2=Lets everyone know how well you did|z=10000;t|s=9999|rg=25|z=500;t|s=9999|rg=25|z=500;t|s=9999|rg=25|z=500;t|s=9999|rg=25|z=500;t|s=9999|rg=25|z=500;t|s=9999|rg=25|z=500;t|s=9999|rg=25|z=500;t|s=9999|rg=25|z=500;|s=9999|t=30|rg=1|1=Mows Pure and Hallowed grass|2=Mowed grass reduces enemy spawn chance|z=10000;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;avt|s=9999|r=3|rg=1|1=Sustain a bloody fraction of the Moon|z=50000;a|s=9999|rg=1|1=The wearer can run super fast, and even faster on sand|2=Walk without rhythm and you wont attract the worm|z=50000;a|s=9999|rg=1|1=Increases mining speed by 25%|2=Ancient problems require ancient solutions|z=50000;|s=9999|r=3|t=12|rg=1|1=Playable Instrument|2=These licks are spicy|z=10000;r|s=9999|r=3|d=8|t=17|k=5|z=100000;d|s=9999|d=8|t=18|k=4|tp=55|rg=1|z=15000;r|s=9999|r=3|d=60|t=18|k=5|rg=1|1=Sure, Brain, but where are you going to get enough stars for this?|z=500000;d|s=9999|d=14|t=28|k=6|rg=1|z=15000;m|s=9999|d=20|t=17|k=3|m=6|rg=1|z=15000;t|s=9999|rg=1|1=Badum, psh|z=100000;t|s=9999|rg=1|z=400;t|s=9999|rg=1|z=500;|s=9999|rg=1|z=25000;|s=9999|rg=1|1=Allows quick travel in water|2=Deal with it|z=50000;t|s=9999|rg=3|1=Hey! Listen!|z=50000;t|s=9999|rg=3|1=Hey! Listen!|z=50000;t|s=9999|rg=3|1=Hey! Listen!|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=10000;t|s=9999|rg=1|z=10000;t|s=9999|rg=1|z=2000;t|s=9999|rg=1|z=5000;t|s=9999|r=3|rg=1|1={$CommonItemTooltip.PersonalStorage}|2=Will contain items picked up by a Void Bag|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;t|s=9999|rg=1|z=2000;t|s=9999|rg=1|z=2000;t|s=9999|rg=1|z=2000;t|s=9999|rg=1|z=2000;t|s=9999|rg=1|z=2000;t|s=9999|rg=1|z=2000;t|s=9999|r=2|rg=1|1=Right Click to place a golf ball on it|z=10000;t|s=9999|rg=100|1={$CommonItemTooltip.CanBeExtractinated}|z=500;t|s=9999|rg=100|1=Portals cannot be created on the surface of these blocks|z=100;|s=9999|r=2|t=12|rg=1|1={$CommonItemTooltip.GolfPutter}|z=10000;|s=9999|r=2|t=12|rg=1|1={$CommonItemTooltip.GolfWedge}|z=10000;|s=9999|r=2|t=12|rg=1|1={$CommonItemTooltip.GolfDriver}|z=10000;|s=9999|r=4|rg=1|1=Returns your last hit Golf Ball to its previous position|2=Has a one stroke penalty|z=100000;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|1=How desperate are you?|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=100000;4v|s=9999|r=3|rg=1|z=25000;5v|s=9999|r=3|rg=1|z=25000;6v|s=9999|r=3|rg=1|z=25000;|s=9999|r=3|t=28|rg=1|1=Summons the Void Vault|2={$CommonItemTooltip.PersonalStorage}|3=Functions as an extended inventory when open|4=May pick up overflowing items when open|5={$CommonItemTooltip.RightClickToClose}|6=This pocket dimension is out of this world!|z=100000;4v|s=9999|r=3|rg=1|z=25000;5v|s=9999|r=3|rg=1|z=25000;6v|s=9999|r=3|rg=1|z=25000;4v|s=9999|r=3|rg=1|z=25000;5v|s=9999|r=3|rg=1|z=25000;6v|s=9999|r=3|rg=1|z=25000;4v|s=9999|r=3|rg=1|z=25000;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=150;t|s=9999|rg=1|1=Used for special crafting|z=100000;|s=9999;d|s=9999|r=2|d=17|t=25|k=3|rg=1|1=Unleashes a dicing flurry|z=250000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;avt|s=9999|r=4|rg=1|1=Brought to you by Xenon and DJ Sniper|z=100000;t|s=9999|rg=100|1=May break when stepped on;t|s=9999|rg=100|1=May break when stepped on;t|s=9999|rg=100|1=May break when stepped on;t|s=9999|rg=25|z=500;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;a|s=9999|r=2|rg=1|1={$CommonItemTooltip.GolfBall}|z=10000;5|s=9999|r=2|D=3|rg=1|1=Increases maximum mana by 60|2=13% reduced mana cost|z=150000;|s=9999|t=20|k=7|rg=1|1={$CommonItemTooltip.Hook}|z=20000;d|s=9999|d=26|t=18|k=3|rg=1|z=27000;d|s=9999|r=4|d=50|t=16|k=3|rg=1|z=50000;w|s=9999|rg=400|z=800;t|s=9999|rg=5|1=Signals and breaks when stepped on by a player|z=5000;|s=9999|t=90|rg=1|1=Summons rope snakes|z=50000;|s=9999|t=90|rg=1|1=If you listen closely, you can hear the ocean|z=50000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable Golf Cart mount|z=500000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;s|s=9999|r=4|d=35|t=36|k=3|rg=1|1=Summons a sanguine bat to fight for you|z=250000;m|s=9999|r=4|d=34|t=33|k=1|m=20|rg=1|1=Summons blood thorns from the ground|z=200000;c|s=9999|r=2|t=45|rg=3|1=Summons the Blood Moon|2=What a horrible night to have a curse.;d|s=9999|r=4|d=55|t=40|k=6.5|rg=1|z=200000;s|s=9999|r=3|d=11|t=36|k=5|rg=1|1=Summons a vampire frog to fight for you|z=50000;t|s=9999|r=3|rg=3|1=Not to be confused with the elusive Gold Gold Goldfish|z=500000;4vt|s=9999|r=3|rg=1|1=Is it a gold bowl? Or a gold fish?|z=500000;t|s=9999|rg=1|1=Increases defense by 5 when placed nearby|z=100000;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;s|s=9999|d=7|t=36|k=4|rg=1|1=Summons a baby finch to fight for you|z=50000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Youll apricate this!|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Good source of potassium!|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=A vegan option for vampires|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Are you suggesting that coconuts can migrate?|z=10000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Smells like your father|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=When life gives you lemons...|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Goes great with pizza|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|z=10000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;d|s=9999|r=4|d=30|t=27|k=7|tx=30|th=80|rg=1|z=100000;avt|s=9999|r=9|rg=1|1=Harness a small amount of power from the Void|z=1250000;t|s=9999|rg=1|1=Right Click to change directions|z=2000;t|s=9999|rg=1|1=Right Click to change directions|z=2000;5v|s=9999|r=3|rg=1|1=I thought I told you to clean up your room!|z=50000;6v|s=9999|r=3|rg=1|1=I thought I told you to clean up your room!|z=50000;4v|s=9999|r=3|rg=1|z=500000;5v|s=9999|r=3|rg=1|z=500000;|s=9999|r=2|t=8|tf=25|rg=1|1=Increased chance to fish up enemies during a Blood Moon|z=100000;t|s=9999|rg=1|1=Right Click to place item on plate|z=150;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|r=3|rg=1|z=500000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|r=3|rg=3|z=500000;a|s=9999|rg=1|1=Hold Up to reach higher|z=25000;t|s=9999|rg=1|z=300;rc|s=9999|d=4|t=17|k=2|rg=1|z=5000;rc|s=9999|d=4|t=17|k=2|rg=1|z=5000;|s=9999|rg=2|1={$CommonItemTooltip.RightClickToOpen}|z=2500;|s=9999|rg=1|1=Prevents item pickups while locked|2=Right Click to unlock|3=You are over-encumbered|z=50000;m|s=9999|r=2|d=42|t=36|k=6|m=16|rg=1|1=It might be broken|z=170000;m|s=9999|r=5|d=100|t=36|k=6|m=16|rg=1|1=It might be broken|z=500000;t|s=9999|rg=25;t|s=9999|rg=25;t|s=9999|rg=25;t|s=9999|rg=25;t|s=9999|rg=25;t|s=9999|rg=25|1=A sight to dwell upon and never forget|2={$CommonItemTooltip.CanBeExtractinated};t|s=9999|rg=1|z=300;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;t|s=9999|rg=5|z=3750;t|s=9999|rg=1|z=300;t|s=9999|rg=5|z=5000;t|s=9999|r=3|rg=3|z=500000;t|s=9999|rg=5|z=2500;t|s=9999|rg=1;|s=9999|r=5|t=20|rg=1|1=Summons Estee|z=1000000;|s=9999|r=3|t=20|rg=1|1=Summons a Pet Sugar Glider|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;|s=9999|r=3|t=12|rg=1|1=Playable Instrument|2=Property of Dead Mans Sweater|z=10000;t|s=9999|rg=5|z=3750;t|s=9999|rg=5|z=3750;t|s=9999|rg=5|z=5000;t|s=9999|rg=1;t|s=9999|rg=25|1=A sight to dwell upon and never forget|2={$CommonItemTooltip.CanBeExtractinated};t|s=9999|rg=25|1=A sight to dwell upon and never forget|2={$CommonItemTooltip.CanBeExtractinated};t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1;r|s=9999|r=3|d=14|t=19|k=3|rg=1|1=Rains blood from the sky|2=Reign in Blood|z=50000;c|s=9999|r=2|t=45|rg=1|1=Increases the defense and strength of all villagers|2=Contains offensive and defensive fighting techniques;t|s=9999|rg=100|z=60;t|s=9999|rg=100|1=Can be placed in water|z=60;t|s=9999|rg=100|z=60;t|s=9999|rg=100|z=60;t|s=9999|rg=100|z=60;t|s=9999|rg=100|z=60;t|s=9999|rg=25|1=A sight to dwell upon and never forget|2={$CommonItemTooltip.CanBeExtractinated};t|s=9999|rg=5|1={$CommonItemTooltip.ContactDamageBlock}|2=They hatin...;t|s=9999|rg=100|1=Breaks when fallen on|2=Watch your step;t|s=9999|rg=100|1=Only visible with Echo Sight|z=1000;|s=9999|rg=2|1=Caught in Desert;|s=9999|rg=2|1=Caught in Desert;t|s=9999|rg=5|z=5000;t|s=9999|rg=1;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=10000;t|s=9999|r=3|rg=1|z=500000;c|s=9999|r=2|t=15|rg=5|1=May drop valuable shinies when smashed!|z=2500;|s=9999|rg=3|z=750;|s=9999|rg=3|1=But it wasnt a rock!|z=5000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Delicious with a bit of butter|z=10000;a|s=9999|rg=1|1=Grants the ability to float in water|z=10000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;a|s=9999|r=5|rg=1|1=Enables Echo Sight, showing hidden blocks|z=100000;|s=9999|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=500;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Aw, shucks!|z=10000;|s=9999|rg=5|z=50000;|s=9999|rg=5|z=150000;|s=9999|rg=5|z=750000;t|s=9999|rg=1|z=200;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;t|s=9999|rg=5|z=5000;t|s=9999|r=3|rg=3|z=500000;t|s=9999|rg=1|1={$CommonItemTooltip.PlacementStyle}|z=50000;avt|s=9999|r=4|rg=1|z=100000;t|s=9999|rg=100|1=Allows only liquids through|2=Closes or opens when signalled|3=Theyre grrrreat!;c|s=9999|t=25|rg=99|1=A narrow explosion that will destroy most tiles|2=Explosion aims away from your position|z=1500;w|s=9999|rg=400;|s=9999|r=5|t=20|rg=1|1=Summons a Shark Pup|2=Doo, doo, doo, doo, doo, doo|z=50000;|s=9999|rg=1|z=50000;|s=9999|rg=1|z=25000;|s=9999|rg=1|1=Powered by Bacon!|z=50000;|s=9999|rg=1|z=25000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;t|s=9999|rg=1|z=30000;|s=9999|t=8|tf=30|rg=1|z=100000;|s=9999|rg=1|1=Provides 7 seconds of immunity to lava|z=50000;|s=9999|r=8|t=20|rg=1|1=Grants a witching spark of inspiration!|z=250000;rb|s=9999|d=50|k=4|rg=99|1=Will not destroy tiles|z=750;rb|s=9999|d=50|k=4|rg=99|1=Will destroy tiles|z=1500;rb|s=9999|d=40|k=4|rg=99|1=Spreads water on impact|z=5000;rb|s=9999|d=40|k=4|rg=99|1=Spreads lava on impact|z=5000;rb|s=9999|d=40|k=4|rg=99|1=Spreads honey on impact|z=5000;|s=9999|rg=1|1=The shroom goes vroom|z=50000;|s=9999|rg=1|z=50000;|s=9999|rg=1|z=50000;|s=9999|rg=1|z=50000;|s=9999|rg=1|z=50000;|s=9999|rg=1|z=50000;|s=9999|rg=1|z=50000;rb|s=9999|d=75|k=4|rg=99|1=Huge blast radius. Will not destroy tiles|z=500;rb|s=9999|d=75|k=4|rg=99|1=Huge blast radius. Will destroy tiles|z=1000;rb|s=9999|d=40|k=4|rg=99|1=Absorbs liquid on impact|z=5000;t|s=9999|rg=1|1=Places sandcastles using Sand Blocks|2={$CommonItemTooltip.PlacementStyle}|z=50000;t|s=9999|rg=1;t|s=9999|rg=1;d|s=9999|d=15|t=18|k=3|rg=1|1=Are you not entertained?!|z=15000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=1|z=300;|s=9999|rg=1|z=50000;|s=9999|rg=1|z=200000;|s=9999|r=10|rg=1|1=brrrrrow|z=500000;|s=9999|rg=1|1=All aboard the party wagon!|z=100000;|s=9999|rg=1|z=50000;|s=9999|rg=1|1=Steam powered!|z=100000;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|r=3|rg=1|z=500000;c|s=9999|t=17|rg=20|1=Increases the Luck of the user|z=50000;c|s=9999|t=17|rg=20|1=Increases the Luck of the user|z=250000;c|s=9999|t=17|rg=20|1=Increases the Luck of the user|z=1250000;t|s=9999|rg=5|z=7500;t|s=9999|rg=1;t|s=9999|r=3|rg=3|z=500000;t|s=9999|r=3|rg=1|z=500000;t|s=9999|rg=1|1=Signals every half of a second|z=20000;t|s=9999|rg=1|1=Signals every fourth of a second|z=20000;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400|z=250;w|s=9999|rg=400|z=250;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400|z=250;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.TheBride}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ZombieMerman}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.EyeballFlyingFish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BloodSquid}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BloodEelHead}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.GoblinShark}|z=1000;t|s=9999|rg=100|z=100;w|s=9999|rg=400;4v|s=9999|r=3|rg=1|z=25000;|s=9999|r=3|t=20|rg=1|1=Summons a baby red panda|z=750000;|s=9999|r=3|t=20|rg=1|1=Summons a baby imp|2=He hasnt learned how to teleport yet!|z=100000;t|s=9999|rg=1|1=Billows out fog|z=40000;t|s=9999|rg=1|z=20000;t|s=9999|rg=50;4v|s=9999|rg=1|1=This chicken is raw!|z=25000;5v|s=9999|rg=1|z=25000;6v|s=9999|rg=1|z=25000;4v|s=9999|r=3|rg=1|z=20000;4v|s=9999|r=3|rg=1|z=20000;4v|s=9999|rg=1|z=30000;4v|s=9999|r=3|rg=1|z=25000;4v|s=9999|r=3|rg=1|1=*tips* Mlady|z=25000;av|s=9999|r=3|rg=1|1=Its a new day, yes it is!|z=25000;t|s=9999|rg=100|z=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;|s=9999|t=12|rg=1|1={$CommonItemTooltip.GolfIron}|z=1000;|s=9999|t=12|rg=1|1={$CommonItemTooltip.GolfPutter}|z=1000;|s=9999|t=12|rg=1|1={$CommonItemTooltip.GolfWedge}|z=1000;|s=9999|t=12|rg=1|1={$CommonItemTooltip.GolfDriver}|z=1000;|s=9999|r=3|t=12|rg=1|1={$CommonItemTooltip.GolfIron}|z=100000;|s=9999|r=3|t=12|rg=1|1={$CommonItemTooltip.GolfPutter}|z=100000;|s=9999|r=3|t=12|rg=1|1={$CommonItemTooltip.GolfWedge}|z=100000;|s=9999|r=3|t=12|rg=1|1={$CommonItemTooltip.GolfDriver}|z=100000;|s=9999|r=4|t=12|rg=1|1={$CommonItemTooltip.GolfIron}|z=250000;|s=9999|r=4|t=12|rg=1|1={$CommonItemTooltip.GolfPutter}|z=250000;|s=9999|r=4|t=12|rg=1|1={$CommonItemTooltip.GolfWedge}|z=250000;|s=9999|r=4|t=12|rg=1|1={$CommonItemTooltip.GolfDriver}|z=250000;t|s=9999|r=3|rg=1|z=10000;t|s=9999|r=3|rg=1|z=10000;t|s=9999|r=3|rg=1|z=10000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BloodNautilus}|z=1000;|s=9999|r=3|t=20|rg=1|1=Summons a baby harpy|2=Not for your everyday cockatiel|z=500000;|s=9999|r=3|t=20|rg=1|1=Summons a fennec fox|2=It squeaks at a glorious 96kHz!|z=1000000;|s=9999|r=3|t=20|rg=1|1=Summons a pet butterfly|2=Only the best, most exquisite flower excrement!|z=1000000;avt|s=9999|r=4|rg=1|z=100000;s|s=9999|r=8|d=41|t=36|k=4|rg=1|1=Summons a white tiger to fight for you|z=1000000;c|s=9999|t=19|rg=25|1=Toss in water up to 3 times to increase fishing power|2=Plankton!|z=2500;t|s=9999|rg=1|1={$CommonItemTooltip.PlacementStyle}|2={$CommonItemTooltip.GoodFortune}|z=50000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Hungry for Apples?|z=10000;c|s=9999|r=4|t=17|rg=5|1={$CommonItemTooltip.MajorStats}|2=Sugar. Water. Purple.|z=40000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=...make lemonade!|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Yellow and mellow|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Lifes a peach|z=10000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=If you like pi\u00f1a coladas and getting caught in the rain|z=20000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Real smooth|z=20000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Not really blood... or is it?|z=20000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Come to the dark side, we have smoothies|z=20000;c|s=9999|r=3|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Feel the rainbow, taste the crystal!|z=20000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=With 5% real fruit juice!|z=20000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Yummy yummy|z=30000;t|s=9999|rg=1|1={$PaintingArtist.UnitOne} (Restored)|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Aurora} (Restored)|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Zoomo}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Darthkitten}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Xman101}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Zoomo}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Khaios}|z=5000;t|s=9999|rg=1|1=Unearthed by C. Schneider|z=5000;t|s=9999|rg=1|1=Unearthed by C. Schneider|z=5000;t|s=9999|rg=1|1={$PaintingArtist.darthmorf}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Leinfors}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Darthkitten}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Darthkitten}|z=5000;t|s=9999|rg=100|z=500;t|s=9999|rg=100|z=500;t|s=9999|rg=100|z=500;t|s=9999|rg=100|z=500;t|s=9999|rg=100|z=500;t|s=9999|rg=100|z=500;t|s=9999|rg=100|z=500;w|s=9999|rg=400;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;4v|s=9999|r=3|rg=1|z=25000;5v|s=9999|r=3|rg=1|z=25000;6v|s=9999|r=3|rg=1|z=25000;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1=Ceci nest pas un club de golf.|2={$PaintingArtist.Crowno}|z=10000;|s=9999|r=3|rg=3|z=75000;|s=9999|r=3|rg=3|z=75000;5v|s=9999|r=3|rg=1|z=200000;6v|s=9999|r=3|rg=1|z=200000;4v|s=9999|r=3|rg=1|z=150000;w|s=9999|rg=400;|s=9999|rg=100|1=Fully illuminates the coated object|2=Can be combined with any other paint or coating|3=What a bright idea!|z=200;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;s|s=9999|d=14|t=30|k=1|rg=1|1=4 summon tag damage|2={$CommonItemTooltip.Whips}|3=Die monster!|z=100000;|s=9999|t=12|rg=1|1=Playable next to Drum Set|2=The coffee is strong...and vulgar|z=5000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;s|s=9999|r=5|d=55|t=28|k=2|rg=1|1=9 summon tag damage|2={$CommonItemTooltip.Whips}|3=Strike enemies to gain whip attack speed|z=230000;s|s=9999|r=8|c=10|d=160|t=35|k=11|rg=1|1=8 summon tag damage|2=10% summon tag critical strike chance|3={$CommonItemTooltip.Whips}|z=300000;s|s=9999|r=8|d=110|t=27|k=3|rg=1|1={$CommonItemTooltip.Whips}|2=Strike enemies with dark energy to gain whip attack speed|3=Dark energy jumps from enemies hit by summons|z=500000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;c|s=9999|t=25|rg=5|1=Released during certain ceremonies|z=100;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|2=This one cant wander too far|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.Kite}|z=20000;4v|s=9999|rg=1|z=100000;5v|s=9999|rg=1|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Dandelion}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Gnome}|z=1000;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=1000;t|s=9999|rg=1|z=40000;t|s=9999|rg=1|z=40000;t|s=9999|rg=1|z=40000;|s=9999|r=3|t=20|rg=1|1=Summons little Plantero|z=500000;c|s=9999|t=25|rg=5|z=100;r|s=9999|r=3|d=14|t=55|k=6.5|rg=1|1=When 2 or 3 just doesnt cut it|z=350000;4v|s=9999|rg=1|z=100000;5v|s=9999|rg=1|z=100000;6v|s=9999|rg=1|z=100000;d|s=9999|r=2|d=15|t=22|k=5|rg=1|1=You will fall slower while holding this|z=100000;4v|s=9999|rg=1|z=100000;5v|s=9999|rg=1|z=100000;t|s=9999|rg=1;d|s=9999|d=12|t=22|k=3.5|rg=1|1=Digs in a bigger area than a pickaxe|2=Only digs up soft tiles|3=Can you dig it?|z=5000;t|s=9999|rg=1|z=2500;t|s=9999|rg=1|z=500;|s=9999|r=8|rg=1|1=Unlocks a Desert Chest in the dungeon;m|s=9999|r=5|c=20|d=85|t=12|k=1.5|m=12|rg=1|1=These chords are out of this world|z=500000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable flamingo mount|z=200000;t|s=9999|rg=50;t|s=9999|rg=50;t|s=9999|rg=50;t|s=9999|rg=50;t|s=9999|rg=50;d|s=9999|r=10|c=10|d=190|t=35|k=6.5|z=1000000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|3=I am right here|z=400000;t|s=9999|r=8|rg=1|1=Seriously? THIS is what you used the Broken Hero Sword for?|z=150;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Could this be the real Ghostar?|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=A fine dress handmade by a dragon|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=The journey of a thousand miles begins with one step|z=250000;|s=9999|r=3|t=20|rg=1|1=Summons a dynamite kitten|2=Its like yarn, but more exciting!|z=500000;|s=9999|r=3|t=20|rg=1|1=Summons a baby werewolf|z=300000;|s=9999|r=3|t=20|rg=1|1=Summons a pet shadow mimic|z=100000;4v|s=9999|r=2|rg=1|z=25000;5v|s=9999|r=2|rg=1|z=25000;4v|s=9999|r=2|rg=1|z=25000;5v|s=9999|r=2|rg=1|z=25000;6v|s=9999|r=2|rg=1|z=25000;c|s=9999|t=15|rg=1|1=Try to catch it!|z=20;av|s=9999|r=5|rg=1|z=150000;|s=9999|rg=1|z=10000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|3=Can you help me tie this on?|z=400000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Bright idea|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Fashionable and functional|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Almost like pants|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|3=The holes cut down on weight.|z=400000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Safety First|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=Max was a good boy.|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=They dont let me wear a loincloth anymore.|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;s|s=9999|r=5|d=6|t=36|rg=1|1=Summons an Enchanted Dagger to fight for you|2=Ignores 25 points of enemy Defense|3=Dont let their small size fool you|z=50000;|s=9999|t=20|k=7|rg=1|1=Grapple onto trees like a real squirrel!|2=In the tree, part of the tree|3={$CommonItemTooltip.Hook}|z=20000;d|s=9999|r=5|d=80|t=36|k=2|rg=1|1=I can do this all day|z=350000;4vt|s=9999|r=3|rg=1;av|s=9999|rg=1|1=Applies dye to minions|z=100000;|s=9999|rg=1|1=Will dig through blocks and lay new track if carrying minecart tracks|2=Only digs when underground|z=500000;d|s=9999|d=23|t=20|k=7|rg=1|z=30000;rc|s=9999|r=2|t=20|rg=1|1=Toss it to change how trees look!|2=Time for a change of scenery|z=30000;rc|s=9999|r=2|t=20|rg=1|1=Toss it to change how the world looks!|2=Time for a change of scenery|z=30000;|s=9999|rg=1|1=Prevents you from hurting critters while in the inventory|2=Right Click to deactivate effects|z=50000;4v|s=9999|rg=1|z=30000;av|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;av|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=30000;av|s=9999|rg=1|z=30000;4v|s=9999|rg=1|z=25000;av|s=9999|rg=1|1=Thats why they call me Thumper|z=30000;c|s=9999|t=15|rg=100|1=Hovers when thrown|2={$CommonItemTooltip.WorksWhenWet}|z=75;|s=9999|r=3|t=20|rg=1|1=Summons a volt bunny|z=500000;|s=9999|r=3|rg=3|z=75000;4v|s=9999|rg=1;5v|s=9999|rg=1;6v|s=9999|rg=1;c|s=9999|r=6|rg=3|1={$CommonItemTooltip.RightClickToOpen};t|s=9999|rg=1|z=50000;4v|s=9999|rg=1|z=37500;|s=9999|r=8|t=20|rg=1|1=Summons a rideable painted horse mount|z=250000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable white horse mount|z=250000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable dark horse mount|z=250000;d|s=9999|r=4|d=60|t=24|k=12|rg=1|1=Build momentum to increase attack power|2=Have at thee!|z=60000;d|s=9999|r=8|d=130|t=24|k=14|rg=1|1=Build momentum to increase attack power|z=500000;d|s=9999|r=5|d=90|t=24|k=13|rg=1|1=Build momentum to increase attack power|z=230000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable pogo stick mount|2=Press Jump again in mid-air to do tricks!|z=250000;|s=9999|t=20|rg=1|1=Summons the Black Spot mount|2=Arrr! This be mutiny!|z=250000;|s=9999|t=20|rg=1|1=Summons a rideable tree mount|2=A wand crafted from the branch of a cursed tree.|z=250000;|s=9999|t=20|rg=1|1=Summons a rideable Santank mount|2=For the REALLY naughty ones.|z=250000;|s=9999|t=20|rg=1|1=Summons a rideable death goat mount|2=Brutal!|z=250000;|s=9999|t=20|rg=1|1=Summons a magic tome mount|2=A book said to be at its holders behest.|z=250000;|s=9999|t=20|rg=1|1=Summons a Slime Prince|2=A dessert fit for a king!|z=250000;|s=9999|t=20|rg=1|1=Summons a suspicious eye|2=Seems to have lost its look.|z=250000;|s=9999|t=20|rg=1|1=Summons the Eater of Worms|2=Itll give you worms!|z=250000;|s=9999|t=20|rg=1|1=Summons a spider brain|2=Pickled and shrunken, this brain can no longer hurt you.|z=250000;|s=9999|t=20|rg=1|1=Summons a small Skeletron|2=A skull with unimaginable power dulled.|z=250000;|s=9999|t=20|rg=1|1=Summons a honey bee|2=The secret ingredient for royal bees.|z=250000;|s=9999|t=20|rg=1|1=Summons a miniature tool of destruction|2=Its safe to operate this, right?|z=250000;|s=9999|t=20|rg=1|1=Summons miniature mechanical eyes|2=Two are better than one.|z=250000;|s=9999|t=20|rg=1|1=Summons a miniature Skeletron Prime|2=We can rebuild it.|z=250000;|s=9999|t=20|rg=1|1=Summons a newly sprouted Plantera|2=Get to the root of the problem.|z=250000;|s=9999|t=20|rg=1|1=Summons a toy golem to light your way|2=Power cells not included.|z=250000;|s=9999|t=20|rg=1|1=Summons a tiny Fishron|2=Brined to perfection.|z=250000;|s=9999|t=20|rg=1|1=Summons a baby phantasmal dragon|2=Contains a fragment of Phantasm energy.|z=250000;|s=9999|t=20|rg=1|1=Summons a Moonling|2=The forbidden calamari.|z=250000;|s=9999|t=20|rg=1|1=Summons a fairy princess to provide light|2=A glowing gemstone that houses a powerful fairy.|z=250000;|s=9999|t=20|rg=1|1=Summons a possessed Jack O Lantern|2=The flame cannot be put out!|z=250000;|s=9999|t=20|rg=1|1=Summons an Everscream sapling|2=Twinkle, Twinkle!|z=250000;|s=9999|t=20|rg=1|1=Summons a tiny Ice Queen|2=Fit for a queen!|z=250000;|s=9999|t=20|rg=1|1=Summons an alien skater|2=Kickflips are a lot easier in Zero-G!|z=250000;|s=9999|t=20|rg=1|1=Summons a baby ogre|2=No other uses besides smashing.|z=250000;|s=9999|t=20|rg=1|1=Summons Itsy Betsy|2=No fire sacrifices required!|z=250000;d|s=9999|r=2|d=25|t=15|k=3.5|rg=1|1=For fixing things, and breaking them|z=25000;|s=9999|r=4|t=90|rg=1|1=If you listen closely, you can hear screams|2=Watch your toes|z=50000;|s=9999|r=7|t=12|tr=2|rg=1|1=Contains an endless amount of lava|2=Can be poured out|z=500000;|s=9999|r=3|t=21|rg=1|1=Used to catch critters and bait|2=Can catch lava critters too!|3=For when things get too hot to handle|z=250000;av|s=9999|r=3|rg=1|1=Leaves a trail of flames in your wake|2=Never get cold feet again|z=100000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|2={$CommonItemTooltip.PressUpToBooster}|z=400000;c|s=9999|t=25|rg=99|1=A small explosion that will spread water|z=2500;c|s=9999|t=25|rg=99|1=A small explosion that will spread lava|z=2500;c|s=9999|t=25|rg=99|1=A small explosion that will spread honey|z=2500;c|s=9999|t=25|rg=99|1=A small explosion that will absorb liquid|z=2500;|s=9999|r=8|t=20|rg=1|1=Summons a rideable lava shark mount|2=Bloody hell!|z=250000;c|s=9999|r=2|t=45|rg=5|1=Use to adopt a cat for your town|2=Already have a cat?|3=Use additional licenses to activate the Pet Exchange Program!|4=Find the perfect fit for you and your cat!|z=50000;c|s=9999|r=2|t=45|rg=5|1=Use to adopt a dog for your town|2=Already have a dog?|3=Use additional licenses to activate the Pet Exchange Program!|4=Find the perfect fit for you and your dog!|z=50000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=5000;t|s=9999|rg=5|z=10000;t|s=9999|rg=1;t|s=9999|rg=5|z=10000;t|s=9999|rg=1;t|s=9999|r=2|rg=5|z=25000;t|s=9999|rg=1;t|s=9999|rg=5|z=3750;t|s=9999|rg=5|z=1875;t|s=9999|rg=5|z=5625;t|s=9999|rg=5|z=7500;t|s=9999|rg=5|z=11250;t|s=9999|rg=5|z=15000;t|s=9999|rg=5|z=15000;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=12500;t|s=9999|rg=1|z=20000;t|s=9999|rg=1|z=20000;c|s=9999|t=30|rg=20|1=Teleports you home and creates a portal|2=Use portal to return when you are done|3=Good for one round trip!|z=1000;t|s=9999|rg=25|z=10000;|s=9999|r=7|t=12|tr=2|rg=1|1=Capable of soaking up an endless amount of lava|z=500000;4|s=9999|r=5|D=1|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 10%|z=250000;a|s=9999|r=7|rg=1|1=Allows flight|2=The wearer can run super fast|3=Leaves a trail of flames in your wake|z=200000;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|2=Requires a Shadow Key|z=20000;t|s=9999|rg=1|1=No, you cant wear it on your head|z=10000;a|s=9999|r=7|rg=1|1={$CommonItemTooltip.LavaFishing}|z=100000;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;4|s=9999|r=5|D=24|rg=1|1=10% increased melee damage and critical strike chance|2=10% increased melee speed|z=250000;4|s=9999|r=5|D=9|rg=1|1=15% increased ranged damage|2=8% increased ranged critical strike chance|z=250000;4|s=9999|r=5|D=5|rg=1|1=Increases maximum mana by 100|2=12% increased magic damage and critical strike chance|z=250000;4|s=9999|r=5|D=1|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 10%|z=250000;5|s=9999|r=5|D=15|rg=1|1=7% increased critical strike chance|z=200000;6|s=9999|r=5|D=11|rg=1|1=7% increased damage|2=8% increased movement speed|z=150000;t|s=9999|r=3|rg=1|z=30000;t|s=9999|r=3|rg=1|z=30000;t|s=9999|r=3|rg=1|z=30000;t|s=9999|r=3|rg=1|z=30000;t|s=9999|r=3|rg=1|z=30000;t|s=9999|rg=25|z=10000;c|s=9999|t=25|rg=99|1=A small explosion that will spread dirt|z=500;c|s=9999|t=25|rg=99|1=A small explosion that will spread dirt|2={$CommonItemTooltip.Sticky}|z=500;c|s=9999|r=2|t=45|rg=5|1=Use to adopt a bunny for your town|2=Already have a bunny?|3=Use additional licenses to activate the Pet Exchange Program!|4=Find the perfect fit for you and your bunny!|z=50000;s|s=9999|r=4|d=45|t=30|k=1.5|rg=1|1=6 summon tag damage|2={$CommonItemTooltip.Whips}|3=Strike enemies to summon a friendly snowflake|4=Let me have some of that cool whip|z=200000;s|s=9999|r=4|d=37|t=30|k=2|rg=1|1={$CommonItemTooltip.Whips}|2=Strike enemies with blazing energy|3=Blazing energy explodes from enemies hit by summons|z=150000;s|s=9999|r=3|d=18|t=30|k=1.5|rg=1|1=6 summon tag damage|2={$CommonItemTooltip.Whips}|3=Strike enemies to gain whip attack speed|z=50000;s|s=9999|r=8|d=170|t=30|k=4|rg=1|1=20 summon tag damage|2=10% summon tag critical strike chance|3={$CommonItemTooltip.Whips}|z=250000;rb|s=9999|d=9|k=4|rg=99|z=18;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.Fountain}|z=40000;d|s=9999|r=8|c=10|d=80|t=18|k=4|rg=1|z=250000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|rg=1|z=50000;t|s=9999|r=9|rg=1|1=Teleport to another pylon|2=Can function anywhere|3=You must construct additional pylons|z=1000000;m|s=9999|r=8|d=50|t=36|k=2.5|m=23|rg=1|z=250000;r|s=9999|r=8|d=50|t=30|k=2|rg=1|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|2={$CommonItemTooltip.PressDownToHover}|3={$CommonItemTooltip.PressUpToBooster}|4=The more you know|z=500000;4v|s=9999|r=5|rg=1|1=It looks like a bunny, but its actually a bunny|z=150000;d|s=9999|r=10|c=10|d=190|t=30|k=6.5|rg=1|z=1000000;c|s=9999|r=6|rg=3|1={$CommonItemTooltip.RightClickToOpen};t|s=9999|rg=1|z=50000;4v|s=9999|rg=1|z=37500;|s=9999|t=20|rg=1|1=Summons a Slime Princess|2=A dessert fit for a queen!|z=250000;t|s=9999|r=3|rg=3|1=Its wings are so delicate, you must be careful not to damage it...|z=250000;t|s=9999|rg=100|1=A Stone Slab variant that merges differently with nearby blocks|2=Favored by advanced builders;t|s=9999|rg=1|1=Wont be running away now...;t|s=9999|rg=1;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.RockGolem}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BloodMummy}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SporeSkeleton}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.SporeBat}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.LarvaeAntlion}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CrimsonBunny}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CrimsonGoldfish}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.CrimsonPenguin}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BigMimicCorruption}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BigMimicCrimson}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.BigMimicHallow}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.MossHornet}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.WanderingEye}|z=1000;a|s=9999|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|z=2000;avt|s=9999|r=4|rg=1|z=100000;|s=9999|r=5|t=20|k=7|rg=1|1=Teleports you to the location of the hook|2={$CommonItemTooltip.Hook}|z=250000;|s=9999|r=8|t=20|rg=1|1=Summons a rideable Winged Slime mount|z=250000;4|s=9999|r=5|D=12|rg=1|1=5% increased critical strike chance|2=10% reduced mana cost|z=100000;5|s=9999|r=5|D=14|rg=1|1=5% increased damage|2=10% chance to save ammo|z=100000;6|s=9999|r=5|D=10|rg=1|1=20% increased movement speed|2=10% increased melee speed|z=100000;avt|s=9999|r=4|rg=1|z=100000;c|s=9999|t=15|rg=50|1=Filled with Party Girl bathwater|z=200;a|s=9999|r=6|rg=1|1=Releases volatile gelatin periodically that damages enemies|z=250000;c|s=9999|r=6|t=45|rg=3|1=Summons Queen Slime|z=50000;a|s=9999|rg=1|1=Grants infinite wing and rocket boot flight|2=Increases flight and jump mobility|z=500000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;t|s=9999|r=9|rg=1|1=Heeellllllo Terraria enthusiasts!|2=Great for chilling like a streamer!|z=25000;4v|s=9999|r=2|rg=1|z=100000;4v|s=9999|r=2|rg=1|z=100000;4v|s=9999|r=2|rg=1|z=100000;5v|s=9999|r=2|rg=1|z=100000;5v|s=9999|r=2|rg=1|z=100000;5v|s=9999|r=2|rg=1|z=100000;a|s=9999|r=7|rg=1|1=Allows flight, super fast running, and extra mobility on ice|2=8% increased movement speed|3=Provides the ability to walk on water, honey & lava|4=Grants immunity to fire blocks and 7 seconds of immunity to lava|5=Reduces damage from touching lava|z=750000;6|s=9999|r=2|D=3|rg=1|1=Slightly increases mobility|2=These might be Steves|z=100000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;t|s=9999|r=2|rg=5|1={$CommonItemTooltip.RightClickToOpen}|z=50000;4v|s=9999|r=9|rg=1|1=You seem to have a problem with your green screen|2=Great for impersonating streamers!|z=15000;s|s=9999|r=5|d=90|t=36|k=4|rg=1|1=Summons an Enchanted Sword to fight for you|z=1000000;avt|s=9999|r=4|rg=1|z=100000;5|s=9999|r=2|D=4|rg=1|1=50% reduced damage and debuff time from traps|z=10000;t|s=9999|rg=1|z=1000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=A must-have for tea parties|z=20;a|s=9999|rg=1|1=Increases pickup range for items|z=150000;d|s=9999|d=9|t=45|k=4.6|rg=1|1=Can be upgraded with torches|z=100000;d|s=9999|d=9|t=45|k=4.6|rg=1|1=May the fire light your way|z=100000;|s=9999;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=For strong, healthy bones|z=10000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Hello darkness my old friend|z=10000;c|s=9999|r=4|t=30|rg=1|1=Unlocks an ability toggle to the left of the inventory|2=When enabled normal torches change according to your biome|z=100000;avt|s=9999|r=4|rg=1|z=100000;4v|s=9999|r=2|rg=1|1=That right there is 100% genuine Ahamkara, trust!|2=Concept by SodaHunter|z=500;5v|s=9999|r=2|rg=1|1=Who knew a blight-weaving world-ender would have such good taste in fashion?|2=Concept by SodaHunter|z=500;6v|s=9999|r=2|rg=1|1=The horrors these boots have seen, or should I say, stepped on.|2=Concept by SodaHunter|z=500;4v|s=9999|r=2|rg=1|1=Your most loyal travel companion. The glowing orb is soft and welcoming.|2=Concept by crowflux|z=500;5v|s=9999|r=2|rg=1|1=Incredibly old, yet unnaturally durable. Who knows how long it traveled just to find you.|2=Concept by crowflux|z=500;6v|s=9999|r=2|rg=1|1=It constantly reminds you that there is more than one path to the top of the mountain.|2=Concept by crowflux|z=500;4v|s=9999|r=2|rg=1|1=May time itself with gentle hands|2=Guide you, who travel on...|3=Concept by DisRicardo|z=500;5v|s=9999|r=2|rg=1|1=...Despite the journeys bygone end...|2=Concept by DisRicardo|z=500;6v|s=9999|r=2|rg=1|1=...And passing of aeons!|2=Concept by DisRicardo|z=500;4v|s=9999|r=2|rg=1|1=When a cosmic ray penetrates a star cloud, that fantastic light...|2=Concept by yikescloud|z=500;5v|s=9999|r=2|rg=1|1=ID.170122 - RSS ZEPHYRUS III|2=Concept by yikescloud|z=500;6v|s=9999|r=2|rg=1|1=I am a duck QUACK-QUACK-QUACK|2=Concept by yikescloud|z=500;4v|s=9999|r=2|rg=1|1=Embodiment of the stars majesty, shiny!|2=Concept by R-MK|z=500;5v|s=9999|r=2|rg=1|1=As protective as glass!|2=Concept by R-MK|z=500;6v|s=9999|r=2|rg=1|1=Now you can wear shoes! Maybe!|2=Right Click to transform|3=Concept by R-MK|z=500;6v|s=9999|r=2|rg=1|1=Sadly, does not provide the ability to swim.|2=Right Click to transform|3=Concept by R-MK|z=500;4v|s=9999|r=2|rg=1|1=You cant see me behind the screen, Im half human and half machine...|2=Concept by Dr.Zootsuit|z=500;5v|s=9999|r=2|rg=1|1=How bad could business possibly be?|2=Concept by Dr.Zootsuit|z=500;6v|s=9999|r=2|rg=1|1=I cant decide... leather or suede?|2=Concept by Dr.Zootsuit|z=500;a|s=9999|r=8|rg=1|1=Fishing line will never break, decreases chance of bait consumption, increases fishing power by 10|2=Allows fishing in lava|z=200000;m|s=9999|r=5|d=70|t=25|k=5|m=18|rg=1|z=500000;t|s=9999|rg=5|z=250;t|s=9999|rg=5|z=250;5|s=9999|r=2|D=1|rg=1|1=Increases summon damage by 5%|2=Increases your max number of minions by 1|z=125000;s|s=9999|r=3|d=8|t=36|k=2|rg=1|1=Summons a snow flinx to fight for you|z=25000;|s=9999|rg=15|1=Its so FLUFFY!|z=500;4v|s=9999|r=5|rg=1|z=500000;5v|s=9999|r=5|rg=1|z=500000;6v|s=9999|r=5|rg=1|z=500000;s|s=9999|r=2|d=29|t=30|k=2|rg=1|1=7 summon tag damage|2={$CommonItemTooltip.Whips}|3=Performs better against multiple targets than most whips|4=This goes to eleven|z=75000;av|s=9999|r=5|rg=1|1=Causes your mouse cursor to have shifting rainbow colors|z=50000;av|s=9999|r=5|rg=1|z=1000000;av|s=9999|r=5|rg=1|z=1000000;5v|s=9999|r=5|rg=1|z=500000;6v|s=9999|r=5|rg=1|z=500000;av|s=9999|r=5|rg=1|z=500000;t|s=9999|r=5|rg=1|z=100000;t|s=9999|r=5|rg=1|z=100000;t|s=9999|r=5|rg=1|z=100000;t|s=9999|r=5|rg=1|z=100000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=100000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=100000;t|s=9999|rg=1|1={$PaintingArtist.Lazure}|z=100000;|s=9999|r=3|t=20|rg=1|1=Summons a cherished teddy bear|2=My childhood buddy - Bernie!|z=250000;|s=9999|r=3|t=20|rg=1|1=Summons a Glommer|2=The petals shimmer in the light|z=250000;|s=9999|t=20|rg=1|1=Summons a tiny Deerclops|2=You lookin at me? Are YOU lookin at ME?|z=250000;|s=9999|r=3|t=20|rg=1|1=Summons a small Pig Man|2=Gross. Its full of hairs.|z=50000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Tastes like hairs and meats with noodle.|z=5000;c|s=9999|r=2|t=17|rg=5|1={$CommonItemTooltip.MediumStats}|2=Has a bit of a kick to it.|z=10000;d|s=9999|r=2|d=20|t=21|k=5.5|rg=1|1=Embeds damaging spikes in enemies|2=Never grab the pointy end.|z=25000;d|s=9999|r=2|c=10|d=27|t=15|k=5|tx=30|rg=1|1=I love Lucy!|z=75000;d|s=9999|r=4|d=57|t=20|k=6.5|rg=1|1=Grows more powerful the better fed you are|2=Defeating enemies temporarily improves healing|3=TIS A WEAPON O PIG BUTT!|z=50000;d|s=9999|r=2|d=36|t=40|k=5.5|rg=1|1=Heals user slightly on hit|2=Maybe I could bat some bats with this.|z=12500;|s=9999|r=3|t=20|rg=1|1=Summons a living chest|2={$CommonItemTooltip.PersonalStorage}|3=Its looking into my soul.|z=100000;4v|s=9999|rg=1|1=It smells like prettiness.|z=500;a|s=9999|rg=1|1=Summons shadow hands to attack your foes|2=Ive got a headache just looking at it.|z=100000;4v|s=9999|rg=1|1=Dont get rain in your eye!|z=25000;5v|s=9999|rg=1|z=20000;6v|s=9999|rg=1|z=20000;av|s=9999|rg=1|1=This is human facial hair.|z=50000;av|s=9999|rg=1|1=This is human facial hair.|z=50000;av|s=9999|rg=1|1=This is human facial hair.|z=50000;a|s=9999|rg=1|1=Increases movement speed and acceleration|2=Provides light when worn|3=A brief light in my dark life.|z=50000;t|s=9999|rg=1|z=50000;4v|s=9999|rg=1|z=37500;t|s=9999|rg=1|z=50000;c|s=9999|r=3|rg=3|1={$CommonItemTooltip.RightClickToOpen};avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|rg=1|1=Allows the user to see the world differently|2=Forbidden Knowledge echoes from the radio...|z=50000;s|s=9999|r=3|d=6|t=36|k=2|rg=1|1=Summons a friendly ghost to fight for you|2=Its hauntingly beautiful.|z=25000;5v|s=9999|rg=1|z=20000;6v|s=9999|rg=1|z=20000;r|s=9999|r=2|d=20|t=15|k=1|rg=1|1=Converts bullets into random stuff|2=ALLOWS WIRELESS TRANSFER OF INJURY!|z=75000;m|s=9999|r=2|d=13|t=45|k=1|m=30|rg=1|1=Ignores 10 points of enemy Defense|2=What a pane... heh|z=75000;sS|s=9999|r=2|d=24|t=30|k=7.5|rg=1|1={$CommonItemTooltip.Sentry}|2=Summons an arcane construct to shoot optic blasts at your enemies|3=Oh me oh my, look at that eye!|z=75000;c|s=9999|t=45|rg=3|1=Summons Deerclops;t|s=9999|rg=1|1={$PaintingArtist.Klei}|2=Adapted by J. T. Kjexrud|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Klei}|2=Adapted by J. T. Kjexrud|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Klei}|2=Adapted by J. T. Kjexrud|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Klei}|2=Adapted by J. T. Kjexrud|z=10000;|s=9999|r=2|rg=1|z=25000;a|s=9999|r=6|rg=1|1=Increases mining speed by 25%|2=Increases block & wall placement speed|3=Increases block placement & tool range by 3|4=Increases pickup range for items|5=Automatically paints or coats placed objects|6=Hold Up to reach higher|z=400000;t|s=9999|rg=25|1=A sight to dwell upon and never forget|2={$CommonItemTooltip.CanBeExtractinated};t|s=9999|rg=25|1=A sight to dwell upon and never forget|2={$CommonItemTooltip.CanBeExtractinated};d|s=9999|r=2|d=15|t=17|k=5|rg=1|1=Best used for pranking townsfolk|2=Makes others smell like cilantro|z=17500;|s=9999|r=8|t=20|rg=1|1=Grants the wearer the power of the wolf|z=250000;|s=9999|t=20|rg=1|1=Summons both Slime Royals|2=A dessert to celebrate love|z=250000;t|s=9999|rg=5|z=5000;t|s=9999|rg=1;|s=9999|r=10|t=30|k=0.3|rg=1|1=Creates and destroys biomes when sprayed|2=Uses colored solution|3=33% chance to save ammo|z=2000000;t|s=9999|r=4|rg=5|1=Activates when signalled|z=30000;4v|s=9999|rg=1|z=25000;t|s=9999|r=2|rg=1|1=When placed in a home, prevents villagers from moving in|2=Smells like cilantro|z=5000;t|s=9999|r=2|rg=1|1=When placed in a home, prevents villagers from moving in|2=Only visible with Echo Sight|3=Smells like expired cilantro|z=5000;a|s=9999|rg=1|1=Increases fishing power by 10|z=25000;a|s=9999|rg=1|1=Increases fishing power by 10|2=Your bobber now glows|z=50000;a|s=9999|rg=1|1=Increases fishing power by 10|2=Your bobber now glows|z=50000;a|s=9999|rg=1|1=Increases fishing power by 10|2=Your bobber now glows|z=50000;a|s=9999|rg=1|1=Increases fishing power by 10|2=Your bobber now glows|z=50000;a|s=9999|rg=1|1=Increases fishing power by 10|2=Your bobber now glows|z=50000;a|s=9999|rg=1|1=Increases fishing power by 10|2=Your bobber now glows|z=50000;a|s=9999|rg=1|1=Increases fishing power by 10|2=Your bobber now glows|z=50000;m|s=9999|c=10|d=15|t=26|m=2|rg=1|1=Shoots a little frost|z=7500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;c|s=9999|r=5|t=17|rg=20|1=Shows the location of infected blocks|z=1000;t|s=9999|rg=5|z=3750;t|s=9999|rg=1;t|s=9999|rg=25|z=150;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;t|s=9999|rg=1|1={$PaintingArtist.darthmorf}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Serenity}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Altermaven}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Jenosis}|z=100000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Redigit}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Kargoh}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Disc}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Aurora}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Serenity}|z=100000;t|s=9999|rg=1|1={$PaintingArtist.UnitOne}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Loki}|2=Im such a lizard, you dont even know my real name...|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Leinfors}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Disc}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Boba}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Jenosis}|z=25000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Grox}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Sigma}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Leinfors}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Aurora}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Suweeka}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Marcus}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Zoomo}|z=30000;t|s=9999|rg=1|1={$PaintingArtist.Boba}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.ManaUser}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Sigma}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Leinfors}|2=In memory of R. N. Ross|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Leinfors}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Serenity}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=25000;t|s=9999|rg=1|1={$PaintingArtist.Jenosis}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Sigma}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Kazzymodus}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Kazzymodus}|z=25000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=25000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Disc}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Xman101}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Cenx}|z=25000;t|s=9999|rg=1|1={$PaintingArtist.Leinfors}|2=Legends say the batteries died ages ago.|z=25000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=25000;t|s=9999|rg=1|1={$PaintingArtist.Crowno}|z=100000;t|s=9999|rg=1|1={$PaintingArtist.Aurora}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Serenity}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=5000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|2=Please Recycle|z=125;|s=9999|r=3|t=20|rg=1|1=Summons a Junimo|2=A mysterious fruit from another world. The flavor is like a dream...|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;4|s=9999|D=2|rg=1;5|s=9999|D=3|rg=1;6|s=9999|D=2|rg=1;r|s=9999|d=10|t=25|rg=1|z=100;d|s=9999|d=9|t=30|k=5.5|th=45|rg=1|z=50;d|s=9999|d=13|t=17|k=5|rg=1|z=100;rc|s=9999|r=2|t=20|rg=1|1=Toss it to change how the moon looks!|2=Time for a change of scenery|z=30000;t|s=9999|r=2|rg=10|z=75000;t|s=9999|r=2|rg=10|z=12500;|s=9999|r=8|rg=1|z=50000;c|s=9999|r=5|t=30|rg=1|1=Permanently grants boosted speed and a defensive probe for minecarts|2=Free Mechanical Cart included!|z=100000;4v|s=9999|rg=1|z=25000;w|s=9999|rg=400|1=Only visible with Echo Sight|z=250;t|s=9999|rg=200|1={$CommonItemTooltip.Platform}|2=Only visible with Echo Sight|z=500;t|s=9999|rg=100|z=60;d|s=9999|r=3|d=24|t=25|k=3.75|rg=1|1=Summons killer bees after striking your foe|z=35000;dt|s=9999|r=4|d=20|k=5|tx=25|rg=1|1=Creates grass on dirt|2=Increases alchemy plant collection when used to gather|3=Plants acorns when cutting down trees|z=75000;t|s=9999|r=7|rg=1|1=Placing some items into the extractinator turns them into something more useful|2=Place contaminated blocks into the extractinator to purify them|3=Other items placed inside may have interesting effects|4=Processes items from adjacent chests when signalled|z=100000;|s=9999|r=3|t=20|rg=1|1=Summons a blue chicken|2=A regular blue chicken egg|z=250000;d|s=9999|r=3|d=21|t=20|k=4.5|rg=1|1=Three Boomerangs are better than one|z=100000;t|s=9999|rg=1|1=Life regen is increased when near a campfire;t|s=9999|rg=5|z=3750;t|s=9999|rg=1;|s=9999|r=7|t=12|tr=2|rg=1|1=Contains an endless amount of honey|2=Can be poured out|z=500000;|s=9999|r=7|t=12|tr=2|rg=1|1=Capable of soaking up an endless amount of honey|z=500000;|s=9999|r=8|t=8|tr=3|rg=1|1=Capable of soaking up an endless amount of liquid|z=1500000;4v|s=9999|rg=1|z=25000;t|s=9999|rg=100|z=100;w|s=9999|rg=400;t|s=9999|rg=1|1={$PaintingArtist.TerrariaCommunity}|z=10000;|s=9999|rg=1|1=Prevents you from accidentally destroying the environment while in the inventory|2=Right Click to deactivate effects|z=50000;t|s=9999|rg=1|1={$PaintingArtist.Yorai}|z=100000;t|s=9999|rg=5|z=3750;t|s=9999|rg=5|z=3750;t|s=9999|rg=5|z=3750;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=30|z=300;t|s=9999|rg=30|z=100;t|s=9999|rg=1|1=Nullifies the peaceful benefits of towns|z=50000;|s=9999|r=2|rg=1|1=Prevents you from hurting critters while in the inventory|2=Prevents you from accidentally destroying the environment while in the inventory|3=Right Click to deactivate effects|z=100000;t|s=9999|r=10|tr=3|rg=1|z=250000;|s=9999|r=3|t=28|rg=1|1=Summons the Void Vault|2={$CommonItemTooltip.PersonalStorage}|3=Functions as an extended inventory when open|4=May pick up overflowing items when open|5={$CommonItemTooltip.RightClickToOpen}|6=This pocket dimension is out of this world!|z=100000;c|s=9999|r=3|t=17|rg=1|1=Consume to permanently increase crafting station range|2=Legendary Bread that once reminded Teddy of home|z=100000;t|s=9999|rg=5|1=Highly volatile;c|s=9999|t=15|rg=5|1=Can be used to lock some chests;t|s=9999|r=10|tr=3|rg=1|z=250000;t|s=9999|r=10|tr=3|rg=1|z=250000;a|s=9999|r=8|rg=1|1=Allows the holder to quadruple jump|2=Increases jump height and negates fall damage|z=200000;|s=9999|r=3|t=20|rg=1|1=Summons Spiffo the Raccoon!|z=100000;|s=9999|r=3|t=20|rg=1|1=Summons a Caveling Gardener|2=Ancient energy in full bloom.|z=100000;|s=9999|r=3|rg=3|1=Summons ???|2=You really shouldnt;|s=9999|r=10|t=20|rg=1|1=Teleports you to the position of the cursor|z=500000;c|s=9999|r=6|t=45|rg=1|1=Increases the defense and strength of all villagers|2=Contains offensive and defensive fighting techniques, volume two!;c|s=9999|r=6|t=45|rg=1|1=Permanently boosts life regeneration|z=75000;c|s=9999|r=6|t=45|rg=1|1=Permanently increases defense|z=100000;c|s=9999|r=6|t=45|rg=1|1=Permanently increases mana regeneration|z=12500;c|s=9999|r=6|t=45|rg=1|1=Permanently increases luck|z=750000;c|s=9999|r=6|t=45|rg=1|1=Permanently increases fishing skill|z=500000;c|s=9999|r=6|t=45|rg=1|1=Permanently increases mining and building speed|z=25000;c|s=9999|r=6|t=45|rg=1|1=Permanently increases items sold by the Traveling Merchant|z=12500;|s=9999|rg=100|1=Renders coated objects visible only with Echo Sight|2=Can be combined with any other paint or coating|3=Its all clear to me now.|z=200;avt|s=9999|r=9|rg=1|1=Enables Echo Sight, showing hidden blocks|z=50000;|s=9999|r=2|rg=1|1=Releases poisonous gas when a container it is in is opened|z=15000;avt|s=9999|r=9|rg=1|1=Witness a glimpse of the Aethers power.|2=Can be used to manifest or suppress the Aether|z=50000;rb|s=9999|r=2|d=12|k=2|rg=99|1=Falls upwards|z=10;t|s=9999|rg=50;t|s=9999|r=5|rg=5|z=1250;t|s=9999|rg=1;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.ShimmerSlime}|z=1000;t|s=9999|rg=100|z=60;a|s=9999|r=5|rg=1|1=Grants immunity to Darkness and Stoned|z=100000;a|s=9999|r=5|rg=1|1=Grants immunity to Shimmer Phasing|2=Hold Down to Phase while submerged in Shimmer|z=100000;|s=9999|rg=1;t|s=9999|rg=1|1=Life regen is increased when near a campfire;|s=9999|r=8|t=90|rg=1|1=Displays everything|2=Allows you to return home at will|3=Right Click to toggle destination|4=If you listen closely, you can hear something about your cars warranty|z=500000;|s=9999|r=8|t=90|rg=1|1=Displays everything|2=Allows you to return to spawn at will|3=Right Click to toggle destination|4=If you listen closely, you can hear something about your cars warranty|z=500000;|s=9999|r=8|t=90|rg=1|1=Displays everything|2=Allows you to travel to the ocean at will|3=Right Click to toggle destination|4=If you listen closely, you can hear something about your cars warranty|z=500000;|s=9999|r=8|t=90|rg=1|1=Displays everything|2=Allows you to travel to the underworld at will|3=Right Click to toggle destination|4=If you listen closely, you can hear something about your cars warranty|z=500000;avt|s=9999|r=4|rg=1|z=100000;w|s=9999|rg=400|1=An egg-infested chunk of Spider wall that will spawn spiders;|s=9999|r=10|t=12|tr=2|rg=1|1=Contains an endless amount of Shimmer|2=Can be poured out|z=500000;w|s=9999|rg=400|1=A cursed segment of Dungeon wall that will spawn monsters;w|s=9999|rg=400|1=A cursed segment of Dungeon wall that will spawn monsters;w|s=9999|rg=400|1=A cursed segment of Dungeon wall that will spawn monsters;w|s=9999|rg=400|1=A cursed segment of Dungeon wall that will spawn monsters;w|s=9999|rg=400|1=A cursed segment of Dungeon wall that will spawn monsters;w|s=9999|rg=400|1=A cursed segment of Dungeon wall that will spawn monsters;w|s=9999|rg=400|1=A cursed segment of Dungeon wall that will spawn monsters;w|s=9999|rg=400|1=A cursed segment of Dungeon wall that will spawn monsters;w|s=9999|rg=400|1=A cursed segment of Dungeon wall that will spawn monsters;w|s=9999|rg=400|1=A dangerous slab of sand wall that will spawn monsters;w|s=9999|rg=400|1=A dangerous slab of sand wall that will spawn monsters;w|s=9999|rg=400|1=An ancient segment of Temple wall that will spawn Lihzahrds;rb|s=9999|d=1|k=1.5|rg=99|1=Exposes nearby treasure|z=150;rb|s=9999|d=1|k=1.5|rg=99|z=7;rb|s=9999|d=1|k=1.5|rg=99|z=7;rb|s=9999|d=1|k=1.5|rg=99|z=7;t|s=9999|r=7|rg=1|1=Allows time to fast forward to dusk one day per week|2=Instantly reusable after certain celestial events|z=150000;d|s=9999|r=5|d=50|t=23|k=4.75|rg=1|1=Its Waffle Time!|z=150000;t|s=9999|r=5|rg=5|z=75;t|s=9999|r=2|rg=5|z=75000;4v|s=9999|rg=1|1=The Blue Traktor is coming|2=Great for impersonating streamers!|z=25000;5v|s=9999|r=3|rg=1|1=Property of Raynbro|z=10000;6v|s=9999|r=3|rg=1|1=Property of Raynbro|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Cheesy}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Cheesy}|z=5000;4v|s=9999|r=3|rg=1|1=Property of Raynbro|z=10000;|s=9999|rg=1|1=Prevents item pickups while locked|2=Right Click to lock|3=You are over-encumbered|z=50000;b|s=9999|r=3|rg=99|1=Used by the Clentaminator|2=Spreads the Desert|z=1500;b|s=9999|r=3|rg=99|1=Used by the Clentaminator|2=Spreads the Snow|z=1500;b|s=9999|r=3|rg=99|1=Used by the Clentaminator|2=Spreads the Forest|z=1500;t|s=9999|rg=100|1={$CommonItemTooltip.CanBeExtractinated};w|s=9999|rg=400;w|s=9999|rg=200;t|s=9999|rg=50;w|s=9999|rg=200;|s=9999|t=20|rg=1|1=Now with 20% more dirt!|z=1000;t|s=9999|r=9|rg=100|1={$CommonItemTooltip.LuminiteVariant};t|s=9999|r=9|rg=100|1={$CommonItemTooltip.LuminiteVariant};t|s=9999|r=9|rg=100|1={$CommonItemTooltip.LuminiteVariant};t|s=9999|r=9|rg=100|1={$CommonItemTooltip.LuminiteVariant};t|s=9999|r=9|rg=100|1={$CommonItemTooltip.LuminiteVariant};t|s=9999|r=9|rg=100|1={$CommonItemTooltip.LuminiteVariant};t|s=9999|r=9|rg=100|1={$CommonItemTooltip.LuminiteVariant};t|s=9999|r=9|rg=100|1={$CommonItemTooltip.LuminiteVariant};w|s=9999|r=9|rg=400|1={$CommonItemTooltip.LuminiteVariant};w|s=9999|r=9|rg=400|1={$CommonItemTooltip.LuminiteVariant};w|s=9999|r=9|rg=400|1={$CommonItemTooltip.LuminiteVariant};w|s=9999|r=9|rg=400|1={$CommonItemTooltip.LuminiteVariant};w|s=9999|r=9|rg=400|1={$CommonItemTooltip.LuminiteVariant};w|s=9999|r=9|rg=400|1={$CommonItemTooltip.LuminiteVariant};w|s=9999|r=9|rg=400|1={$CommonItemTooltip.LuminiteVariant};w|s=9999|r=9|rg=400|1={$CommonItemTooltip.LuminiteVariant};t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100|1={$CommonItemTooltip.BurningBlock};w|s=9999|rg=400;|s=9999|r=8|t=90|rg=1|1=Displays everything|2=Right Click to toggle destination|3=If you listen closely, you can hear something about your cars warranty|z=500000;c|s=9999|t=15|rg=30|1=Causes saplings to instantly grow into trees|2=It\u2019s got what plants crave!|z=50;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;t|s=9999|rg=100;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;w|s=9999|rg=400;|s=9999|r=3|t=32|rg=1|1=Rotate Left/Right to steer & Press Up to accelerate|2=First rule of Flight Club, we dont talk about Flight Club|z=100000;a|s=9999|r=3|rg=1|1=Allows wearer to see through their remote vehicle camera|2=Chonky Fishron RC Systems|z=100000;|s=9999|rg=1|1=Prevents you from hurting critters while in the inventory|2=Effects are currently inactive, Right Click to reactivate|z=50000;|s=9999|rg=1|1=Prevents you from accidentally destroying the environment while in the inventory|2=Effects are currently inactive, Right Click to reactivate|z=50000;|s=9999|r=2|rg=1|1=Prevents you from hurting critters while in the inventory|2=Prevents you from accidentally destroying the environment while in the inventory|3=Effects are currently inactive, Right Click to reactivate|z=100000;s|s=9999|r=2|d=5|t=36|k=2|rg=1|1=Summons a mushroom boi to fight for you|2=Get Mushed Boi!|z=15000;4v|s=9999|r=4|rg=1|1=A mysterious dark cloud, with what appears to be a glowing crystal within...|z=50000;5v|s=9999|r=4|rg=1|1=Helps keep the Malaise at bay|z=25000;6v|s=9999|r=4|rg=1|1=Possession is 9/10 of the law|z=25000;r|s=9999|r=4|d=100|t=40|k=8|rg=1|1=Derelict Distillery, 2020 Vintage|z=200000;m|s=9999|r=4|d=22|t=20|k=5|m=8|rg=1|1=A Dead Mans Hand for your opponents, mon ch\u00e9ri|z=200000;d|s=9999|r=4|d=70|t=15|k=5|rg=1|1=A helping hand from a former prison warden, fallen to the Malaise|z=200000;sS|s=9999|r=4|d=33|t=30|k=7.5|rg=1|1={$CommonItemTooltip.Sentry}|2=Summons a hanging barnacle to poison enemies|z=200000;t|s=9999|r=10|tr=3|rg=1|1=Used with materials to place matching stalactites and stalagmites|2={$CommonItemTooltip.PlacementStyle}|3=I forget which are the ceiling ones and which are the floor ones|z=250000;a|s=9999|r=2|rg=1|1=Jump while holding DOWN to slam downward|2=Slamming into the ground will deal damage to nearby enemies|3=Pound the ground like the heroes of old|z=87500;|s=9999|r=3|t=20|rg=1|1=Summons a pet swarm biter|2=Careful, I hear they bite!|z=100000;t|s=9999|rg=1|1=For display purposes only!|z=25000;t|s=9999|rg=1|1=For display purposes only!|z=25000;t|s=9999|rg=1|1=For display purposes only!|z=50000;t|s=9999|rg=1|1=For display purposes only!|z=50000;t|s=9999|rg=50|1=Is neither sticky nor broken by weapons|2=For display purposes only!;t|s=9999|rg=1;s|s=9999|d=9|t=35|k=0.7|rg=1|1=3 summon tag damage|2={$CommonItemTooltip.Whips}|3=Strike enemies to summon a tiny spider|z=750;s|s=9999|d=17|t=30|k=1.35|rg=1|1=5 summon tag damage|2={$CommonItemTooltip.Whips}|3=Lash your enemies with the remnants of evil incarnate|z=50000;s|s=9999|d=19|t=30|k=1.25|rg=1|1=5 summon tag damage|2={$CommonItemTooltip.Whips}|3=Arteries, veins, and sinew!|z=50000;s|s=9999|r=2|d=18|t=30|k=1.5|rg=1|1=2 summon tag damage|2={$CommonItemTooltip.Whips}|3=Strike enemies with primed energy|4=Meteorites fall on primed enemies hit by summons|z=125000;s|s=9999|r=7|d=75|t=30|k=2|rg=1|1=9 summon tag damage|2={$CommonItemTooltip.Whips}|3=Strike enemies with volatile energy|4=Violent petals bloom from enemies hit by summons|5=A thorn-barbed whip ripped from the Queen of the Jungle herself? Metal!|z=300000;s|s=9999|r=8|d=150|t=30|k=3|rg=1|1=12 summon tag damage|2=5% summon tag critical strike chance|3={$CommonItemTooltip.Whips}|4=Strike enemies with electric energy|5=Electric energy resonates between enemies|z=250000;s|s=9999|r=10|d=130|t=30|k=4|rg=1|1=15 summon tag damage|2=15% summon tag critical strike chance|3={$CommonItemTooltip.Whips}|4=Strike enemies to scatter friendly stars|z=500000;s|s=9999|r=10|d=110|t=30|k=4|rg=1|1=25 summon tag damage|2=10% summon tag critical strike chance|3={$CommonItemTooltip.Whips}|4=Strike enemies with prophetic energy|5=Visions strike enemies hit by summons|z=500000;t|s=9999|r=10|tr=3|rg=1|1=Used with materials to place matching decorative pots|2={$CommonItemTooltip.PlacementStyle}|3=Is a portable pot machine a porta-potty?|z=250000;t|s=9999|rg=1|1={$CommonItemTooltip.UseableWhenPlaced}|2={$BuffDescription.DeadCellsPotionStation}|3=A mysterious bottle from far away|z=100000;t|s=9999|rg=1|1={$PaintingArtist.Shadou}|z=5000;av|s=9999|rg=1|1=Just the prescription you needed|z=50000;av|s=9999|rg=1|1=Who you calling chicken?|z=50000;t|s=9999|rg=1|1={$PaintingArtist.Burr}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Martin}|z=25000;t|s=9999|rg=1|1={$PaintingArtist.Lime}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lime}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Lira}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Myhre}|z=50000;t|s=9999|rg=1|z=200;t|s=9999|rg=100;w|s=9999|rg=400;|s=9999|r=5|t=20|rg=1|1=Squirts an enchanted stream of Shimmer|2=Transform your friends! Does not contain psycho-reactive slime|z=15000;c|s=9999|r=7|hl=180|t=17|rg=30|1=A tropical concoction with hints of Life Fruit and nutmeg|z=105000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=500;av|s=9999|rg=1|1=Ribbit til you croak!|z=50000;av|s=9999|rg=1|1=Quite the gruff tuft|z=50000;av|s=9999|rg=1|1=Otto was here|z=50000;av|s=9999|rg=1|1=Dont give a crap attitude not included|z=50000;av|s=9999|rg=1|1=Bark: not just for trees anymore|z=50000;av|s=9999|rg=1|1=You have a sudden urge for cranberry sauce|z=50000;av|s=9999|rg=1|1=Oh, where have all the gobbos gone?|z=50000;av|s=9999|rg=1|1=You may not be able to quote the raven, but this is pretty close|z=50000;av|s=9999|rg=1|1=We all float down here...|z=50000;av|s=9999|rg=1|1=Braaiiiinnnssss...|z=50000;av|s=9999|rg=1|1=Yes, you do say blah blah blah|z=50000;|s=9999|r=8|t=20|rg=1|1=Embrace your inner raptor - complete with nasty, big, pointy teeth!|z=75000;t|s=9999|rg=5|1=Pufferfish are friends, not food|z=5000;t|s=9999|rg=1;|s=9999|r=2|t=20|rg=1|1=Summons a friendly pufferfish|2=The PB&J of the sea|z=50000;t|s=9999|r=5|rg=5|1=Shiny. Sparkly. Deadly.|z=25000;5v|s=9999|r=2|rg=1|1=The legs, the torso, the whole darn thing...|z=100000;t|s=9999|rg=5|1=I do what I want;|s=9999|r=3|t=20|rg=1|1=Summons Cenaxe|2=Bears a striking resemblance to a certain someone...|z=100000;c|s=9999|t=15|rg=30|1=Causes saplings to instantly grow into taller trees|2=It\u2019s got what plants crave!|z=50;d|s=9999|r=4|d=20|t=20|k=5|tx=30|rg=1|1=Reach out and chop someone|z=75000;t|s=9999|rg=5;t|s=9999|rg=5;t|s=9999|rg=5;|s=9999|t=20|rg=1|1=Summons a friendly boulder to roll after you|2=Lets rock n roll!|z=1000;4|s=9999|r=7|D=2|rg=1|1=Increases your max number of minions by 1|2=Increases summon damage by 12%|z=300000;|s=9999|r=8|t=20|rg=1|1=Inflicts the bearer with the nature of the rat|z=75000;d|s=9999|r=8|d=66|t=20|k=4.5|rg=1|1=Spews homing bubbles|2=Right Click to toggle modes|z=250000;t|s=9999|rg=1|1={$PaintingArtist.Gejdel}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Egdom}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Waasephi}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Sangsuwan}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Forestia}|z=10000;t|s=9999|rg=1|1=This is the real deal;t|s=9999|rg=1|1=This is the real deal;av|s=9999|rg=1|1=Clap your hands!|z=50000;d|s=9999|d=26|t=18|k=3|rg=1|z=27000;d|s=9999|r=4|d=50|t=16|k=3|rg=1|z=50000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=1500;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;a|s=9999|r=5|rg=1|1=Allows offstring yoyo tricks|z=1000000;a|s=9999|r=5|rg=1|1=Gives the user magic yoyo skills|z=1500000;c|s=9999|t=25|rg=99|1=A small explosion that will freeze water|z=1000;a|s=9999|rg=1|1=Continuously use some items when not moving|2=Not functional when inventory is open|z=50000;t|s=9999|rg=200|1={$CommonItemTooltip.Platform}|2={$CommonItemTooltip.PreventFallDamage};w|s=9999|rg=400|1=A dangerous slab of living wood wall that will spawn monsters;w|s=9999|rg=400|1=A dangerous clump of dirt wall that will spawn monsters;a|s=9999|r=3|rg=1|1={$CommonItemTooltip.String}|2={$CommonItemTooltip.Counterweight}|z=51500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform}|2=Does not block items;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=100|1={$CommonItemTooltip.PreventFallDamage};t|s=9999|rg=100|1={$CommonItemTooltip.PreventFallDamage};t|s=9999|rg=100|1={$CommonItemTooltip.PreventFallDamage};c|s=9999|d=8|k=5.75|rg=100;c|s=9999|r=2|t=17|rg=20|1=Nearby torches will be converted to match the biome|z=1000;|s=9999|rg=1|1=Magically delicious! Please do not eat.|z=25000;|s=9999|rg=1|1=Watering this lucky charm with Shimmer maybe wasnt the best idea...;|s=9999|rg=1|1=A passing raven chose you to carry this emblem.|2=What manner of ill doth this portent?;c|s=9999|d=13|t=25|k=3|rg=99|1=You break it, you buy it - you and all of your friends|z=10000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;av|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;4|s=9999|r=4|D=10|rg=1|1={$ItemTooltip.MiningHelmet}|z=125000;5|s=9999|r=4|D=12|rg=1|1={$ItemTooltip.MiningShirt}|z=125000;6|s=9999|r=4|D=11|rg=1|1={$ItemTooltip.MiningPants}|z=125000;4|s=9999|r=4|D=10|rg=1|1={$ItemTooltip.AnglerHat}|2=You are the captain now|z=125000;5|s=9999|r=4|D=12|rg=1|1={$ItemTooltip.AnglerVest}|z=125000;6|s=9999|r=4|D=11|rg=1|1={$ItemTooltip.AnglerPants}|z=125000;c|s=9999|r=4|t=40|rg=99|1=A large explosion that will destroy most tiles|2=Can destroy hardmode ores|z=1000;c|s=9999|r=4|t=40|rg=99|1={$ItemTooltip.SuperBomb}|2={$CommonItemTooltip.Sticky}|z=1000;av|s=9999|r=4|rg=1|z=50000;|s=9999|r=8|t=20|rg=1|1=Inflicts the bearer with the curse of the bat|z=250000;avt|s=9999|r=9|rg=1|1=Behold the ancient power of the CRT Monitorlith|z=250000;avt|s=9999|r=9|rg=1|1=Witness a fraction of visuals past|z=250000;|s=9999|r=3|t=20|rg=1|1={$CommonItemTooltip.RollerSkate}|z=150000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=100;w|s=9999|rg=400;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.TerrariaEnthusiasts}|2=To be worn only in battle|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.TerrariaEnthusiasts}|2=Stylish, and functional|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.TerrariaEnthusiasts}|2=Trousers, not pants|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.TerrariaEnthusiasts}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;av|s=9999|r=9|rg=1|1={$CommonItemTooltip.TerrariaEnthusiasts}|2=The remains of a Red Hat|z=250000;|s=9999|d=5|t=20|k=4|rg=1|1=Fires acorns that plant themselves when they land|z=10000;t|s=9999|rg=1|1={$PaintingArtist.TerrariaCommunity}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=5000;t|s=9999|rg=1|1=The lunatic is on the grass... The lunatic is in my head...|2={$PaintingArtist.Shadou}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.TerrariaCommunity}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Criddle}|z=5000;t|s=9999|rg=1|1={$PaintingArtist.Shadou}|z=10000;t|s=9999|rg=1|1={$PaintingArtist.TerrariaCommunity}|z=10000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;avt|s=9999|r=4|rg=1|z=100000;|s=9999|r=3|t=20|rg=1|1={$CommonItemTooltip.RollerSkate}|z=100000;|s=9999|r=3|t=20|rg=1|1={$CommonItemTooltip.RollerSkate}|z=100000;|s=9999|r=3|t=20|rg=1|1={$CommonItemTooltip.RollerSkate}|z=100000;c|s=9999|t=15|rg=100|1={$CommonItemTooltip.WorksWhenWet}|z=20;|s=9999|t=100|rg=1|1=See through the eyes of other players|z=50000;c|s=9999|t=17|rg=5|1={$CommonItemTooltip.MinorStats}|z=10000;5v|s=9999|rg=1|z=20000;6v|s=9999|rg=1|z=20000;5v|s=9999|rg=1|z=20000;6v|s=9999|rg=1|z=20000;6v|s=9999|rg=1|z=20000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.Orca}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;t|s=9999|rg=1|1={$CommonItemTooltip.TeleportationPylon}|z=100000;|s=9999|t=20|rg=1|1=Summons a friendly rainbow boulder to roll after you|2=This rock is poppin!|z=1000;avt|s=9999|r=9|rg=1|1=Projects a view of classic cinema|z=50000;4v|s=9999|r=9|rg=1|1=Believe in yourself, and value others. Both are blessings.|z=250000;5v|s=9999|r=9|rg=1|1=Draw wisdom from many places, lest it become stale.|z=250000;6v|s=9999|r=9|rg=1|1=True humility is the only antidote to shame.|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.FlightAndSlowfall}|2=At every setback, begin again... wiser.|z=400000;4v|s=9999|r=5|rg=1|z=250000;|s=9999|r=9|rg=1|1=Can be activated with the right materials|2=At every setback, begin again... wiser.|z=125000;|s=9999|r=8|t=20|rg=1|1=Grants the user an air of a pixie|2=Great idea, Sprite! Too bad it\u2019s terrible|z=250000;s|s=9999|r=5|d=9|t=15|k=2|rg=1|1=Summons Cattiva to fight for you|2=A lovely pal that also helps your mining efforts|3=Right Click to pet it!|z=50000;s|s=9999|r=4|d=20|t=15|k=3|rg=1|1=Summons Foxparks to fight for you|2=Left Click to use Foxsparks Huggy Fire skill, shooting flames!|3=Right Click to pet it!|z=50000;|s=9999|t=20|rg=1|1=Summons Chillet for a ride!|2=Empowers your attacks while mounted.|3=Double tap a direction to use Rocket Slam!|4=Right Click to pet it!|z=500;|s=9999|r=5|t=20|rg=1|1=Summons Chillet Ignis for a ride!|2=Empowers your attacks while mounted.|3=Double tap a direction to use Rocket Slam!|4=Right Click to pet it!|z=500;|s=9999|r=3|t=15|rg=1|1=Summons Digtoise to help mining!|2=Left Click to direct Digtoise|3=Right Click away to recall Digtoise|4=Right Click to pet it!|z=50000;|s=9999|1=How do you have this?;d|s=9999|r=10|c=10|d=190|t=30|k=6.5|rg=1|z=1000000;d|s=9999|d=26|t=18|k=3|rg=1|z=27000;d|s=9999|r=4|d=50|t=16|k=3|rg=1|z=50000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.LibrarianSkeleton}|z=1000;t|s=9999|rg=1|1={$CommonItemTooltip.BannerBonus}{$NPCName.WaterBoltMimic}|z=1000;t|s=9999|rg=100|z=100;t|s=9999|rg=100|z=100;t|s=9999|rg=100|z=100;t|s=9999|rg=100|z=100;t|s=9999|rg=100|z=100;t|s=9999|rg=100|z=100;4v|s=9999|r=2|rg=1|z=25000;5v|s=9999|r=2|rg=1|z=25000;6v|s=9999|r=2|rg=1|z=25000;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2=I have a cunning plan...|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;d|s=9999|d=9|t=24|k=6.5|rg=1|z=250;s|s=9999|d=12|t=30|k=1|rg=1|1=3 summon tag damage|2=Whip crack can ignite enemies|z=250;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=100;w|s=9999|rg=400;5v|s=9999|r=3|rg=1|1=Armor fully utilizing the latest otherworldy technology.|2=Maybe its overwhelming defense is the pals you made along the way?|z=50000;6v|s=9999|r=3|rg=1|1=Armor fully utilizing the latest otherworldy technology.|2=Maybe its overwhelming defense is the pals you made along the way?|z=50000;|s=9999|r=9|rg=1|1={$CommonItemTooltip.TerrariaEnthusiasts}|2=Can be activated with the right materials|z=125000;|s=9999|r=3|t=32|rg=1|1=Press Jump to drive on the walls if they are behind the car|2=Last rule of Car Club, we always talk about Car Club|z=100000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;w|s=9999|rg=400;t|s=9999|rg=100|z=50;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100|z=50;w|s=9999|rg=400;t|s=9999|rg=100|1=Breaks most toys when touched|2=NO FUN ALLOWED!|z=200;t|s=9999|rg=1|1={$PaintingArtist.Lime}|z=5000;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=1|z=300;t|s=9999|rg=1|1={$CommonItemTooltip.Bed}|z=2000;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=1500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=3000;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=200;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=500;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=300;t|s=9999|rg=200|1={$CommonItemTooltip.Platform};t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=300;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=150;t|s=9999|rg=1|z=500;t|s=9999|rg=100;w|s=9999|rg=400;t|s=9999|rg=100|1={$CommonItemTooltip.ContactDamageBlock};|s=9999;|s=9999;4v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;5v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;6v|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;a|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|2={$CommonItemTooltip.FlightAndSlowfall}|z=400000;av|s=9999|r=9|rg=1|1={$CommonItemTooltip.DevItem}|z=250000;|s=9999|r=3|rg=1|1=May hatch into something wonderful!|2={$CommonItemTooltip.RightClickToOpen}|z=50000;|s=9999;avt|s=9999|r=4|rg=1|z=100000;|s=9999'.split(";");
    M.PLegendary2 = 84;
    M.PFabled = 85;
    M.PLoyal = 86;
    M.PWorthy = 87;
    M.PFocused = 88;
    M.PPatient = 89;
    M.PRabid = 90;
    M.PIllTempered = 91;
    M.PPetty = 92;
    M.PFeeble = 93;
    M.PSkittish = 94;
    M.PEager = 95;
    M.PBallistic = 96;
    M.PScraggling = 97;
    Da._rect = new Ia;
    Da._offset = new ia;
    cb.map = new Y;
    t.main()
})("undefined" != typeof console ? console : {
    log: function() {}
}, "undefined" != typeof window ? window : exports, "undefined" != typeof window ? window : "undefined" != typeof global ? global : "undefined" != typeof self ? self : this);