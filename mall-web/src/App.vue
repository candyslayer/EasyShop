<script setup>
import { computed, onMounted, ref } from 'vue'
import { loadCatalog } from './api'
import { ArrowRight, Check, ChevronDown, Clock3, Heart, Leaf, Menu, Minus, Plus, Search, ShoppingBag, Sparkles, Truck, UserRound, X } from '@lucide/vue'

const activeCategory = ref('全部')
const search = ref('')
const cartOpen = ref(false)
const menuOpen = ref(false)
const favoriteIds = ref([])

const categories = ['全部', '当季鲜果', '有机蔬菜', '海鲜水产', '肉禽蛋品', '乳品烘焙', '粮油调味']
const products = ref([
  { id: 1, name: '云南蓝莓', note: '果香浓郁 · 12盒装', price: 39, oldPrice: 49, category: '当季鲜果', image: 'https://images.unsplash.com/photo-1498557850523-fd3d118b962e?auto=format&fit=crop&w=900&q=85', tag: '今日新鲜' },
  { id: 2, name: '晨露生菜', note: '清晨采摘 · 约300g', price: 12.8, oldPrice: 16, category: '有机蔬菜', image: 'https://images.unsplash.com/photo-1622205313162-be1d5712a43b?auto=format&fit=crop&w=900&q=85', tag: '有机' },
  { id: 3, name: '深海白虾', note: '肉质紧实 · 500g', price: 59, oldPrice: 69, category: '海鲜水产', image: 'https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?auto=format&fit=crop&w=900&q=85', tag: '人气' },
  { id: 4, name: '日晒西红柿', note: '自然熟成 · 约1kg', price: 18.9, oldPrice: 24, category: '有机蔬菜', image: 'https://images.unsplash.com/photo-1546470427-227c7369e9a8?auto=format&fit=crop&w=900&q=85', tag: '限时折扣' },
  { id: 5, name: '高原牦牛奶', note: '醇厚奶香 · 6瓶装', price: 42, oldPrice: 48, category: '乳品烘焙', image: 'https://images.unsplash.com/photo-1563636619-e9143da7973b?auto=format&fit=crop&w=900&q=85', tag: '新品' },
  { id: 6, name: '黑猪小里脊', note: '谷饲鲜切 · 300g', price: 36, oldPrice: 45, category: '肉禽蛋品', image: 'https://images.unsplash.com/photo-1602470520998-f4a52199a3d6?auto=format&fit=crop&w=900&q=85', tag: '安心肉' }
])
const cart = ref([{ ...products.value[0], quantity: 1 }])

const filteredProducts = computed(() => products.value.filter(product => (activeCategory.value === '全部' || product.category === activeCategory.value) && product.name.includes(search.value.trim())))
const cartCount = computed(() => cart.value.reduce((sum, item) => sum + item.quantity, 0))
const cartTotal = computed(() => cart.value.reduce((sum, item) => sum + item.price * item.quantity, 0))

onMounted(async () => {
  try {
    const remote = await loadCatalog()
    if (remote.categories?.length) categories.splice(1, categories.length - 1, ...remote.categories.map(item => item.name))
    if (remote.products?.length) products.value = remote.products.map(item => ({
      id: item.id, name: item.name, note: item.subtitle || '拾味精选', price: item.minPrice, oldPrice: item.maxPrice || item.minPrice,
      category: categories.find(category => category !== '全部') || '全部', image: item.primaryImageUrl || 'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=900&q=85', tag: '新鲜到家'
    }))
  } catch { /* 允许前端在 API 未启动时使用演示数据 */ }
})

function addToCart(product) {
  const existing = cart.value.find(item => item.id === product.id)
  if (existing) existing.quantity += 1
  else cart.value.push({ ...product, quantity: 1 })
  cartOpen.value = true
}
function changeQuantity(item, amount) {
  item.quantity += amount
  if (item.quantity <= 0) cart.value = cart.value.filter(cartItem => cartItem.id !== item.id)
}
function toggleFavorite(id) {
  favoriteIds.value = favoriteIds.value.includes(id) ? favoriteIds.value.filter(item => item !== id) : [...favoriteIds.value, id]
}
</script>

<template>
  <div class="app-shell">
    <header class="site-header">
      <a class="brand" href="#top"><span class="brand-mark"><Leaf :size="20" /></span><span>拾味</span><small>EASY SHOP</small></a>
      <nav class="main-nav" :class="{ open: menuOpen }"><a class="active" href="#shop" @click="menuOpen = false">逛一逛</a><a href="#fresh" @click="menuOpen = false">今日鲜选</a><a href="#group" @click="menuOpen = false">拼团特惠</a></nav>
      <div class="header-actions"><button class="icon-button desktop-only"><Search :size="19" /></button><button class="icon-button" @click="cartOpen = true"><ShoppingBag :size="19" /><b>{{ cartCount }}</b></button><button class="account-button"><UserRound :size="17" /><span>登录</span></button><button class="menu-button" :aria-expanded="menuOpen" aria-label="打开导航" @click="menuOpen = !menuOpen"><X v-if="menuOpen" :size="20" /><Menu v-else :size="20" /></button></div>
    </header>

    <main id="top">
      <section class="hero" id="shop">
        <div class="hero-copy"><p class="eyebrow"><Sparkles :size="15" /> 今天也要好好吃饭</p><h1>把一日三餐，<em>交给新鲜。</em></h1><p class="hero-text">从清晨田间到你家餐桌，我们只挑当季最好的那一口。</p><button class="primary-button" @click="document.querySelector('#fresh').scrollIntoView({ behavior: 'smooth' })">开始挑选 <ArrowRight :size="17" /></button></div>
        <div class="hero-note"><span>01</span><div><strong>晨间采摘</strong><small>每日 06:00 更新</small></div></div>
      </section>

      <section class="service-strip"><div><Truck :size="20" /><span><strong>满 59 元</strong> 免配送费</span></div><div><Clock3 :size="20" /><span><strong>最快 30 分钟</strong> 送到家</span></div><div><Check :size="20" /><span><strong>不满意包退</strong> 新鲜保障</span></div></section>

      <section class="promo-section" id="group"><div class="promo-copy"><p class="eyebrow">本周限定 · WEEKLY PICK</p><h2>一桌春色，<br /><em>现在正好。</em></h2><p>时令水果、清晨蔬菜和一份适合分享的好心情，都在这一篮里。</p><button class="text-button">去看看 <ArrowRight :size="16" /></button></div><div class="promo-image"><img src="https://images.unsplash.com/photo-1610832958506-aa56368176cf?auto=format&fit=crop&w=1400&q=88" alt="新鲜水果" /><span>本周鲜选<br /><strong>任选 3 件 8 折</strong></span></div></section>

      <section class="catalog-section" id="fresh"><div class="section-heading"><div><p class="eyebrow">FRESH FROM THE SOURCE</p><h2>今天吃什么？</h2></div><div class="search-field"><Search :size="17" /><input v-model="search" placeholder="搜索商品" /><kbd>⌘ K</kbd></div></div><div class="category-row"><button v-for="category in categories" :key="category" :class="{ selected: activeCategory === category }" @click="activeCategory = category">{{ category }}</button><ChevronDown :size="17" class="category-more" /></div><div class="product-grid"><article v-for="product in filteredProducts" :key="product.id" class="product-item"><div class="product-image"><img :src="product.image" :alt="product.name" /><span class="product-tag">{{ product.tag }}</span><button class="favorite-button" :class="{ liked: favoriteIds.includes(product.id) }" @click="toggleFavorite(product.id)"><Heart :size="17" :fill="favoriteIds.includes(product.id) ? 'currentColor' : 'none'" /></button><button class="quick-add" @click="addToCart(product)"><Plus :size="17" /></button></div><div class="product-meta"><div><h3>{{ product.name }}</h3><p>{{ product.note }}</p></div><div class="price"><strong>¥{{ product.price }}</strong><del>¥{{ product.oldPrice }}</del></div></div></article></div></section>

      <section class="closing-section"><div><p class="eyebrow">GOOD FOOD, GOOD MOOD</p><h2>今晚，做一道<br /><em>值得记住的菜。</em></h2></div><button class="outline-button">探索全部商品 <ArrowRight :size="17" /></button></section>
    </main>

    <footer class="site-footer"><div class="footer-brand"><span class="brand-mark"><Leaf :size="18" /></span><strong>拾味</strong><p>认真生活，从认真吃饭开始。</p></div><div class="footer-links"><a>关于拾味</a><a>配送说明</a><a>售后保障</a><a>成为分销员</a></div><span class="copyright">© 2026 EASY SHOP</span></footer>

    <Transition name="drawer"><div v-if="cartOpen" class="drawer-backdrop" @click.self="cartOpen = false"><aside class="cart-drawer"><div class="drawer-top"><div><p class="eyebrow">YOUR BAG</p><h2>购物袋 <span>({{ cartCount }})</span></h2></div><button class="icon-button" @click="cartOpen = false"><X :size="20" /></button></div><div v-if="cart.length" class="cart-lines"><div v-for="item in cart" :key="item.id" class="cart-line"><img :src="item.image" :alt="item.name" /><div class="cart-line-info"><strong>{{ item.name }}</strong><small>{{ item.note }}</small><div class="line-bottom"><span>¥{{ item.price }}</span><div class="quantity"><button @click="changeQuantity(item, -1)"><Minus :size="13" /></button><b>{{ item.quantity }}</b><button @click="changeQuantity(item, 1)"><Plus :size="13" /></button></div></div></div></div></div><div v-else class="empty-cart"><ShoppingBag :size="28" /><p>购物袋还是空的</p><small>去挑几样今天的新鲜吧</small></div><div class="drawer-bottom"><div><span>合计</span><strong>¥{{ cartTotal.toFixed(2) }}</strong></div><button class="primary-button">去结算 <ArrowRight :size="17" /></button></div></aside></div></Transition>
  </div>
</template>
