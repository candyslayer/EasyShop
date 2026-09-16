const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

export async function loadCatalog() {
  const [categoriesResponse, productsResponse] = await Promise.all([
    fetch(`${API_BASE}/api/categories`),
    fetch(`${API_BASE}/api/products?page=1&pageSize=12`)
  ])
  if (!categoriesResponse.ok || !productsResponse.ok) throw new Error('catalog unavailable')
  const categories = await categoriesResponse.json()
  const products = await productsResponse.json()
  return { categories, products: products.items || [] }
}
