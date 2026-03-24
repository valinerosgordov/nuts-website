/* ============================================
   Ореховый Сад — Main JS
   Premium interactions & effects
   ============================================ */

document.addEventListener('DOMContentLoaded', () => {

  // =========================================
  // Floating Particles (Canvas)
  // =========================================
  const canvas = document.getElementById('particles');
  if (canvas) {
    const ctx = canvas.getContext('2d');
    let particles = [];
    let animId;

    const resize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    resize();
    window.addEventListener('resize', resize);

    class Particle {
      constructor() {
        this.reset();
      }
      reset() {
        this.x = Math.random() * canvas.width;
        this.y = Math.random() * canvas.height;
        this.size = Math.random() * 2 + 0.5;
        this.speedX = (Math.random() - 0.5) * 0.3;
        this.speedY = (Math.random() - 0.5) * 0.3;
        this.opacity = Math.random() * 0.15 + 0.05;
        // Golden tones
        const tone = Math.random();
        if (tone < 0.6) {
          this.color = `rgba(201, 168, 76, ${this.opacity})`;      // gold
        } else if (tone < 0.85) {
          this.color = `rgba(139, 105, 20, ${this.opacity})`;      // dark gold
        } else {
          this.color = `rgba(212, 184, 106, ${this.opacity * 0.7})`;// light gold
        }
      }
      update() {
        this.x += this.speedX;
        this.y += this.speedY;
        if (this.x < -10 || this.x > canvas.width + 10 ||
            this.y < -10 || this.y > canvas.height + 10) {
          this.reset();
        }
      }
      draw() {
        ctx.beginPath();
        ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
        ctx.fillStyle = this.color;
        ctx.fill();
      }
    }

    const count = Math.min(80, Math.floor(window.innerWidth / 20));
    for (let i = 0; i < count; i++) {
      particles.push(new Particle());
    }

    const animate = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      particles.forEach(p => {
        p.update();
        p.draw();
      });
      animId = requestAnimationFrame(animate);
    };
    animate();

    // Pause particles when tab is hidden
    document.addEventListener('visibilitychange', () => {
      if (document.hidden) {
        cancelAnimationFrame(animId);
      } else {
        animate();
      }
    });
  }

  // =========================================
  // Falling Golden Leaves (CSS-animated SVGs)
  // =========================================
  if (window.innerWidth >= 768 && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    const LEAF_COLORS = ['#c9a84c', '#8b6914', '#d4b86a', '#a07d2e'];
    // Realistic walnut leaf with serrated edges
    const LEAF_PATHS = [
      // Walnut leaf shape 1 — classic pointed with serrations
      'M12 1C12 1 10 3 8.5 5.5C7.5 4.8 6 5 5.5 6.5C4.5 6 3 6.8 3 8.5C2 8.5 1 9.8 1.5 11.5C1 12 0.8 13.5 2 14.5C1.5 15.5 2 17 3.5 17.5C3.5 18.5 4.5 19.5 6 19.5C6.5 20.5 7.5 21.5 9 21.5C9.5 22.5 10.5 23 12 23C13.5 23 14.5 22.5 15 21.5C16.5 21.5 17.5 20.5 18 19.5C19.5 19.5 20.5 18.5 20.5 17.5C22 17 22.5 15.5 22 14.5C23.2 13.5 23 12 22.5 11.5C23 9.8 22 8.5 21 8.5C21 6.8 19.5 6 18.5 6.5C18 5 16.5 4.8 15.5 5.5C14 3 12 1 12 1Z',
      // Leaf shape 2 — rounder, maple-like
      'M12 1.5C12 1.5 9 4 7 6C5.5 5 3.5 5.5 3 7.5C1.5 7.5 0.5 9 1 11C0 12 0 14 1.5 15.5C1 17 2 19 4 19.5C4.5 21 6.5 22 8.5 21.5C10 23 14 23 15.5 21.5C17.5 22 19.5 21 20 19.5C22 19 23 17 22.5 15.5C24 14 24 12 23 11C23.5 9 22.5 7.5 21 7.5C20.5 5.5 18.5 5 17 6C15 4 12 1.5 12 1.5Z',
      // Leaf shape 3 — elongated, elegant
      'M12 0.5C12 0.5 10 2.5 9 4.5C7.5 4 6 4.5 5.5 6C4 6 3 7 3 8.5C2 9 1.5 10.5 2 12C1.5 13 1.5 14.5 2.5 15.5C2.5 17 3.5 18 5 18.5C5.5 19.5 7 20.5 8.5 20.5C9.5 21.5 10.5 22.5 12 22.5C13.5 22.5 14.5 21.5 15.5 20.5C17 20.5 18.5 19.5 19 18.5C20.5 18 21.5 17 21.5 15.5C22.5 14.5 22.5 13 22 12C22.5 10.5 22 9 21 8.5C21 7 20 6 18.5 6C18 4.5 16.5 4 15 4.5C14 2.5 12 0.5 12 0.5Z'
    ];
    const MAX_LEAVES = 12;
    let activeLeaves = 0;

    const createLeaf = () => {
      if (activeLeaves >= MAX_LEAVES) return;

      const size = Math.random() * 15 + 15; // 15–30px
      const left = Math.random() * 100;      // 0–100vw
      const duration = Math.random() * 8 + 12; // 12–20s
      const delay = Math.random() * 2;
      const color = LEAF_COLORS[Math.floor(Math.random() * LEAF_COLORS.length)];
      const swayDir = Math.random() > 0.5 ? 1 : -1;

      const ns = 'http://www.w3.org/2000/svg';
      const svg = document.createElementNS(ns, 'svg');
      svg.setAttribute('viewBox', '0 0 24 24');
      svg.setAttribute('width', String(size));
      svg.setAttribute('height', String(size));
      svg.classList.add('falling-leaf');
      svg.style.left = left + 'vw';
      svg.style.animationDuration = duration + 's';
      svg.style.animationDelay = delay + 's';
      svg.style.transform = `scaleX(${swayDir})`;

      const path = document.createElementNS(ns, 'path');
      path.setAttribute('d', LEAF_PATHS[Math.floor(Math.random() * LEAF_PATHS.length)]);
      path.setAttribute('fill', color);
      path.setAttribute('opacity', '0.75');
      svg.appendChild(path);

      // Leaf vein line for realism
      const vein = document.createElementNS(ns, 'line');
      vein.setAttribute('x1', '12');
      vein.setAttribute('y1', '4');
      vein.setAttribute('x2', '12');
      vein.setAttribute('y2', '20');
      vein.setAttribute('stroke', color);
      vein.setAttribute('stroke-opacity', '0.3');
      vein.setAttribute('stroke-width', '0.5');
      svg.appendChild(vein);

      document.body.appendChild(svg);
      activeLeaves++;

      const cleanup = () => {
        svg.remove();
        activeLeaves--;
      };

      svg.addEventListener('animationend', cleanup);
      // Safety fallback in case animationend doesn't fire
      setTimeout(cleanup, (duration + delay + 1) * 1000);
    };

    // Stagger initial batch
    for (let i = 0; i < MAX_LEAVES; i++) {
      setTimeout(createLeaf, i * 1500);
    }

    // Continuously spawn new leaves
    setInterval(() => {
      if (activeLeaves < MAX_LEAVES) {
        createLeaf();
      }
    }, 2000);
  }

  // =========================================
  // Header scroll effect
  // =========================================
  const header = document.getElementById('header');

  const onScroll = () => {
    header.classList.toggle('header--scrolled', window.scrollY > 60);
  };
  window.addEventListener('scroll', onScroll, { passive: true });

  // =========================================
  // Mobile menu
  // =========================================
  const burger = document.getElementById('burger');
  const mobileMenu = document.getElementById('mobileMenu');

  burger.addEventListener('click', () => {
    burger.classList.toggle('active');
    mobileMenu.classList.toggle('active');
    document.body.style.overflow = mobileMenu.classList.contains('active') ? 'hidden' : '';
  });

  mobileMenu.querySelectorAll('a').forEach(link => {
    link.addEventListener('click', () => {
      burger.classList.remove('active');
      mobileMenu.classList.remove('active');
      document.body.style.overflow = '';
    });
  });

  // =========================================
  // Reveal on scroll (IntersectionObserver)
  // =========================================
  const revealElements = document.querySelectorAll('.reveal');

  const revealObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const parent = entry.target.parentElement;
        const siblings = parent ? Array.from(parent.querySelectorAll(':scope > .reveal')) : [];
        const index = siblings.indexOf(entry.target);
        const delay = index >= 0 ? index * 120 : 0;

        setTimeout(() => {
          entry.target.classList.add('revealed');
        }, delay);

        revealObserver.unobserve(entry.target);
      }
    });
  }, {
    threshold: 0.12,
    rootMargin: '0px 0px -60px 0px'
  });

  revealElements.forEach(el => revealObserver.observe(el));

  // =========================================
  // Catalog category tabs
  // =========================================
  const tabs = document.querySelectorAll('.catalog__tab');
  const catalogItems = document.querySelectorAll('.catalog__item[data-category]');

  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      tabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');
      const cat = tab.dataset.category;

      catalogItems.forEach(item => {
        const cats = item.dataset.category.split(' ');
        if (cat === 'all' || cats.includes(cat)) {
          item.classList.remove('catalog__item--hidden');
        } else {
          item.classList.add('catalog__item--hidden');
        }
      });
    });
  });

  // =========================================
  // Counter animation (stats numbers)
  // =========================================
  const counters = document.querySelectorAll('.stats__number[data-target]');

  const counterObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const el = entry.target;
        const target = parseInt(el.dataset.target, 10);
        const suffix = el.dataset.suffix || '';
        const duration = 2000;
        const start = performance.now();

        const easeOutQuart = t => 1 - Math.pow(1 - t, 4);

        const step = (now) => {
          const elapsed = now - start;
          const progress = Math.min(elapsed / duration, 1);
          const value = Math.floor(easeOutQuart(progress) * target);
          el.textContent = value.toLocaleString('ru-RU') + suffix;
          if (progress < 1) {
            requestAnimationFrame(step);
          }
        };
        requestAnimationFrame(step);
        counterObserver.unobserve(el);
      }
    });
  }, { threshold: 0.5 });

  counters.forEach(el => counterObserver.observe(el));

  // =========================================
  // Smooth scroll for anchor links
  // =========================================
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', (e) => {
      const target = document.querySelector(anchor.getAttribute('href'));
      if (target) {
        e.preventDefault();
        const headerHeight = header.offsetHeight;
        const targetPosition = target.getBoundingClientRect().top + window.scrollY - headerHeight - 20;
        window.scrollTo({ top: targetPosition, behavior: 'smooth' });
      }
    });
  });

  // =========================================
  // Parallax on hero tree
  // =========================================
  const treeWrapper = document.querySelector('.hero__tree-wrapper');
  if (treeWrapper) {
    window.addEventListener('scroll', () => {
      const scrollY = window.scrollY;
      if (scrollY < window.innerHeight) {
        treeWrapper.style.transform = `translateY(${scrollY * 0.15}px)`;
      }
    }, { passive: true });
  }

  // =========================================
  // Mouse glow on cards
  // =========================================
  const cards = document.querySelectorAll('.philosophy__card, .catalog__item, .media__card');
  cards.forEach(card => {
    card.addEventListener('mousemove', (e) => {
      const rect = card.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      card.style.background = `radial-gradient(400px circle at ${x}px ${y}px, rgba(139,105,20,0.07), var(--black-card))`;
    });
    card.addEventListener('mouseleave', () => {
      card.style.background = '';
    });
  });

  // =========================================
  // Magnetic effect on CTA buttons
  // =========================================
  const magneticButtons = document.querySelectorAll('.hero__cta, .catalog__button, .contact__submit');
  magneticButtons.forEach(btn => {
    btn.addEventListener('mousemove', (e) => {
      const rect = btn.getBoundingClientRect();
      const x = e.clientX - rect.left - rect.width / 2;
      const y = e.clientY - rect.top - rect.height / 2;
      btn.style.transform = `translate(${x * 0.15}px, ${y * 0.15}px)`;
    });
    btn.addEventListener('mouseleave', () => {
      btn.style.transform = '';
      btn.style.transition = 'transform 0.4s cubic-bezier(0.16, 1, 0.3, 1)';
      setTimeout(() => { btn.style.transition = ''; }, 400);
    });
  });

  // Custom cursor removed for better UX

  // =========================================
  // Shopping Cart
  // =========================================
  const escapeHtml = (str) => str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');

  const CART_KEY = 'nuts_cart';

  const getCart = () => {
    try {
      return JSON.parse(localStorage.getItem(CART_KEY)) || [];
    } catch {
      return [];
    }
  };

  const saveCart = (cart) => {
    localStorage.setItem(CART_KEY, JSON.stringify(cart));
  };

  const addToCart = (product) => {
    const cart = getCart();
    const existing = cart.find(
      item => item.name === product.name && item.variant === product.variant
    );
    if (existing) {
      existing.qty += 1;
    } else {
      cart.push({ ...product, qty: 1 });
    }
    saveCart(cart);
    updateCartBadge();
    renderCart();
  };

  const removeFromCart = (index) => {
    const cart = getCart();
    cart.splice(index, 1);
    saveCart(cart);
    updateCartBadge();
    renderCart();
  };

  const updateQuantity = (index, delta) => {
    const cart = getCart();
    if (!cart[index]) return;
    cart[index].qty += delta;
    if (cart[index].qty <= 0) {
      cart.splice(index, 1);
    }
    saveCart(cart);
    updateCartBadge();
    renderCart();
  };

  const updateCartBadge = () => {
    const badge = document.getElementById('cartBadge');
    if (!badge) return;
    const cart = getCart();
    const count = cart.reduce((sum, item) => sum + item.qty, 0);
    badge.textContent = count;
    badge.classList.toggle('visible', count > 0);
  };

  const renderCart = () => {
    const container = document.getElementById('cartItems');
    const footer = document.getElementById('cartFooter');
    const totalEl = document.getElementById('cartTotal');
    if (!container) return;

    const cart = getCart();

    if (cart.length === 0) {
      container.innerHTML = `
        <div class="cart-drawer__empty">
          <svg viewBox="0 0 24 24"><path d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 01-8 0"/></svg>
          <p>Корзина пуста</p>
        </div>`;
      if (footer) footer.style.display = 'none';
      return;
    }

    if (footer) footer.style.display = '';

    let total = 0;
    container.innerHTML = cart.map((item, i) => {
      const lineTotal = item.price * item.qty;
      total += lineTotal;
      return `
        <div class="cart-item">
          <div class="cart-item__info">
            <div class="cart-item__name">${escapeHtml(item.name)}</div>
            <div class="cart-item__variant">${escapeHtml(item.variant)}</div>
            <div class="cart-item__controls">
              <button class="cart-item__qty-btn" data-action="minus" data-index="${i}">&minus;</button>
              <span class="cart-item__qty">${item.qty}</span>
              <button class="cart-item__qty-btn" data-action="plus" data-index="${i}">+</button>
            </div>
            <div class="cart-item__price">${lineTotal.toLocaleString('ru-RU')} &#8381;</div>
          </div>
          <button class="cart-item__remove" data-index="${i}">
            <svg viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
          </button>
        </div>`;
    }).join('');

    if (totalEl) {
      totalEl.textContent = total.toLocaleString('ru-RU') + ' \u20BD';
    }
  };

  // Cart drawer open/close
  const openCart = () => {
    const overlay = document.getElementById('cartOverlay');
    const drawer = document.getElementById('cartDrawer');
    if (overlay) overlay.classList.add('active');
    if (drawer) drawer.classList.add('active');
    document.body.style.overflow = 'hidden';
    renderCart();
  };

  const closeCart = () => {
    const overlay = document.getElementById('cartOverlay');
    const drawer = document.getElementById('cartDrawer');
    if (overlay) overlay.classList.remove('active');
    if (drawer) drawer.classList.remove('active');
    document.body.style.overflow = '';
  };

  const cartToggle = document.getElementById('cartToggle');
  if (cartToggle) cartToggle.addEventListener('click', openCart);

  const cartClose = document.getElementById('cartClose');
  if (cartClose) cartClose.addEventListener('click', closeCart);

  const cartOverlay = document.getElementById('cartOverlay');
  if (cartOverlay) cartOverlay.addEventListener('click', closeCart);

  // Delegated events for cart items
  const cartItemsEl = document.getElementById('cartItems');
  if (cartItemsEl) {
    cartItemsEl.addEventListener('click', (e) => {
      const qtyBtn = e.target.closest('.cart-item__qty-btn');
      if (qtyBtn) {
        const index = parseInt(qtyBtn.dataset.index, 10);
        const delta = qtyBtn.dataset.action === 'plus' ? 1 : -1;
        updateQuantity(index, delta);
        return;
      }
      const removeBtn = e.target.closest('.cart-item__remove');
      if (removeBtn) {
        removeFromCart(parseInt(removeBtn.dataset.index, 10));
      }
    });
  }

  // Add-to-cart buttons on catalog cards
  document.addEventListener('click', (e) => {
    const addBtn = e.target.closest('.catalog__add-to-cart');
    if (!addBtn) return;

    const card = addBtn.closest('.catalog__item');
    if (!card) return;

    const name = card.querySelector('h3').textContent;
    const activeVariant = card.querySelector('.variant-btn.active') || card.querySelector('.variant-btn');
    let variant, price;
    if (activeVariant) {
      variant = activeVariant.textContent.trim();
      price = parseInt(activeVariant.dataset.price, 10);
    } else {
      variant = '1 шт';
      const priceEl = card.querySelector('.catalog__item-price');
      price = priceEl ? parseInt(priceEl.textContent.replace(/\D/g, ''), 10) || 0 : 0;
    }

    addToCart({ name, variant, price });

    // Visual feedback
    addBtn.classList.add('catalog__add-to-cart--added');
    const origHTML = addBtn.innerHTML;
    addBtn.innerHTML = '<svg viewBox="0 0 24 24"><polyline points="20 6 9 17 4 12"/></svg> Добавлено';
    setTimeout(() => {
      addBtn.classList.remove('catalog__add-to-cart--added');
      addBtn.innerHTML = origHTML;
    }, 1200);
  });

  // Init badge on load
  updateCartBadge();

  // =========================================
  // Contact form
  // =========================================
  const form = document.getElementById('contactForm');
  if (form) {
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      const btn = form.querySelector('.contact__submit span');
      const originalText = btn.textContent;
      btn.textContent = 'Отправка...';

      try {
        const messageEl = form.querySelector('[name="message"]');
        const companyEl = form.querySelector('[name="company"]');
        const nameValue = form.querySelector('[name="name"]').value;
        const fullName = companyEl && companyEl.value ? companyEl.value + ' — ' + nameValue : nameValue;
        const body = {
          name: fullName,
          phone: form.querySelector('[name="phone"]').value,
          email: form.querySelector('[name="email"]').value || null,
          message: messageEl ? messageEl.value || null : null
        };
        const res = await fetch('/api/contacts', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(body)
        });
        btn.textContent = res.ok ? 'Отправлено!' : 'Ошибка';
        if (res.ok) form.reset();
      } catch {
        btn.textContent = 'Ошибка сети';
      }
      setTimeout(() => { btn.textContent = originalText; }, 2500);
    });
  }

});
