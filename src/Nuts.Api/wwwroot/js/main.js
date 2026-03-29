/* ============================================
   Ореховый Сад — Main JS
   Premium interactions & effects
   ============================================ */

document.addEventListener('DOMContentLoaded', () => {

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
  // Mouse glow on cards — removed per client request
  // =========================================

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
  const CART_TS_KEY = 'nuts_cart_ts';
  const CART_TTL = 10 * 24 * 60 * 60 * 1000; // 10 days

  const getCart = () => {
    try {
      const ts = parseInt(localStorage.getItem(CART_TS_KEY) || '0');
      if (Date.now() - ts > CART_TTL) {
        localStorage.removeItem(CART_KEY);
        localStorage.removeItem(CART_TS_KEY);
        return [];
      }
      return JSON.parse(localStorage.getItem(CART_KEY)) || [];
    } catch {
      return [];
    }
  };

  const saveCart = (cart) => {
    localStorage.setItem(CART_KEY, JSON.stringify(cart));
    localStorage.setItem(CART_TS_KEY, Date.now().toString());
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

  // Expose addToCart globally for product.html
  window.addToCart = addToCart;

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

  // =========================================
  // Drag scroll on media ticker
  // =========================================
  const ticker = document.querySelector('.media__ticker-wrapper');
  if (ticker) {
    let isDown = false, startX, scrollLeft;

    ticker.addEventListener('mousedown', (e) => {
      isDown = true;
      ticker.style.cursor = 'grabbing';
      startX = e.pageX - ticker.offsetLeft;
      scrollLeft = ticker.scrollLeft;
      // Pause CSS animation while dragging
      const track = ticker.querySelector('.media__ticker-track');
      if (track) track.style.animationPlayState = 'paused';
    });

    ticker.addEventListener('mouseleave', () => { isDown = false; ticker.style.cursor = 'grab'; });
    ticker.addEventListener('mouseup', () => { isDown = false; ticker.style.cursor = 'grab'; });

    ticker.addEventListener('mousemove', (e) => {
      if (!isDown) return;
      e.preventDefault();
      const x = e.pageX - ticker.offsetLeft;
      ticker.scrollLeft = scrollLeft - (x - startX) * 2;
    });

    // Touch support
    ticker.addEventListener('touchstart', (e) => {
      startX = e.touches[0].pageX;
      scrollLeft = ticker.scrollLeft;
      const track = ticker.querySelector('.media__ticker-track');
      if (track) track.style.animationPlayState = 'paused';
    }, { passive: true });

    ticker.addEventListener('touchmove', (e) => {
      const x = e.touches[0].pageX;
      ticker.scrollLeft = scrollLeft - (x - startX);
    }, { passive: true });

    ticker.addEventListener('touchend', () => {
      const track = ticker.querySelector('.media__ticker-track');
      if (track) track.style.animationPlayState = 'running';
    });

    ticker.style.cursor = 'grab';
  }

  // =========================================
  // Promo popup
  // =========================================
  const promoPopup = document.getElementById('promoPopup');
  if (promoPopup) {
    fetch('/api/banners/active').then(r => {
      if (r.status === 204 || !r.ok) return null;
      return r.json();
    }).then(banner => {
      if (!banner) return;
      const shown = sessionStorage.getItem('promo_shown_' + banner.id);
      if (shown) return;

      setTimeout(() => {
        document.getElementById('promoTitle').textContent = banner.title;
        document.getElementById('promoDesc').textContent = banner.description;
        if (banner.buttonText && banner.buttonUrl) {
          const btn = document.getElementById('promoBtn');
          btn.textContent = banner.buttonText;
          btn.href = banner.buttonUrl;
          btn.style.display = 'inline-block';
        }
        promoPopup.style.display = 'flex';
        sessionStorage.setItem('promo_shown_' + banner.id, '1');
      }, (banner.delaySeconds || 3) * 1000);
    }).catch(() => {});

    document.getElementById('promoClose')?.addEventListener('click', () => { promoPopup.style.display = 'none'; });
    promoPopup.querySelector('.promo-popup__overlay')?.addEventListener('click', () => { promoPopup.style.display = 'none'; });
  }

  // =========================================
  // Search overlay
  // =========================================
  const searchToggle = document.getElementById('searchToggle');
  const searchOverlay = document.getElementById('searchOverlay');
  const searchInput = document.getElementById('searchInput');
  const searchClose = document.getElementById('searchClose');
  const searchResults = document.getElementById('searchResults');

  if (searchToggle && searchOverlay) {
    const pages = [
      { title: 'Главная', url: '/', keywords: 'ореховый сад премиум орехи главная' },
      { title: 'Каталог', url: '/catalog.html', keywords: 'каталог товары орехи сухофрукты мармелад десерты грецкий миндаль фундук кешью фисташка' },
      { title: 'Оптовые продажи', url: '/wholesale.html', keywords: 'опт оптовые продажи бизнес' },
      { title: 'Подарочные наборы', url: '/gifts.html', keywords: 'подарки наборы корпоративные подарочные' },
      { title: 'О компании', url: '/about.html', keywords: 'о нас компания история команда' },
      { title: 'Доставка и оплата', url: '/delivery.html', keywords: 'доставка оплата скидки москва' },
      { title: 'Гарантия качества', url: '/warranty.html', keywords: 'гарантия возврат качество сертификат' },
      { title: 'FAQ', url: '/faq.html', keywords: 'вопросы ответы faq помощь' },
      { title: 'Контакты', url: '/contacts.html', keywords: 'контакты телефон адрес whatsapp telegram' },
      { title: 'Личный кабинет', url: '/account.html', keywords: 'кабинет профиль заказы вход регистрация' },
    ];

    searchToggle.addEventListener('click', () => {
      searchOverlay.classList.add('active');
      setTimeout(() => searchInput.focus(), 100);
    });

    searchClose.addEventListener('click', () => {
      searchOverlay.classList.remove('active');
      searchInput.value = '';
      searchResults.innerHTML = '';
    });

    searchOverlay.addEventListener('click', (e) => {
      if (e.target === searchOverlay) {
        searchOverlay.classList.remove('active');
      }
    });

    const escapeHtml = (s) => s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');

    searchInput.addEventListener('input', () => {
      const q = searchInput.value.toLowerCase().trim();
      if (!q) { searchResults.innerHTML = ''; return; }
      const matches = pages.filter(p =>
        p.title.toLowerCase().includes(q) || p.keywords.includes(q)
      );
      searchResults.innerHTML = matches.length
        ? matches.map(p =>
            `<a href="${p.url}" class="search-result"><div class="search-result__title">${escapeHtml(p.title)}</div><div class="search-result__type">Страница</div></a>`
          ).join('')
        : '<div class="search-result"><div class="search-result__title">Ничего не найдено</div></div>';
    });

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') searchOverlay.classList.remove('active');
    });
  }

});
