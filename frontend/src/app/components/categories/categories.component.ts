import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { FinisApiService } from '../../services/finis-api.service';
import { Category, TransactionType } from '../../models/models';

@Component({
  selector: 'app-categories',
  templateUrl: './categories.component.html',
  styleUrls: ['./categories.component.css']
})
export class CategoriesComponent implements OnInit, OnDestroy {
  categories: Category[] = [];
  filteredCategories: Category[] = [];
  selectedTypeFilter: number = 0; // 0 = Todas
  loading = false;
  errorMessage = '';
  private refreshSub?: Subscription;

  // Modal State
  isModalOpen = false;
  modalTitle = 'Nova Categoria';
  editingCategoryId: number | null = null;
  formName = '';
  formType: TransactionType = TransactionType.Diario;
  formColor = '#10B981';
  formIcon = 'tag';
  modalLoading = false;
  modalError = '';

  // Palette of vibrant, clean fintech colors
  colorPalette = [
    '#10B981', '#059669', '#34D399', '#3B82F6', '#2563EB',
    '#6366F1', '#8B5CF6', '#A855F7', '#EC4899', '#F43F5E',
    '#EF4444', '#DC2626', '#F97316', '#F59E0B', '#64748B'
  ];

  // Common financial icons
  availableIcons = [
    { id: 'tag', label: 'Etiqueta' },
    { id: 'wallet', label: 'Carteira' },
    { id: 'cash-stack', label: 'Dinheiro' },
    { id: 'credit-card', label: 'Cartão' },
    { id: 'piggy-bank', label: 'Poupança' },
    { id: 'cart', label: 'Mercado' },
    { id: 'cup-hot', label: 'Café / Restaurante' },
    { id: 'house', label: 'Moradia' },
    { id: 'lightning', label: 'Energia' },
    { id: 'droplet', label: 'Água' },
    { id: 'wifi', label: 'Internet' },
    { id: 'car-front', label: 'Transporte' },
    { id: 'heart-pulse', label: 'Saúde' },
    { id: 'controller', label: 'Lazer' },
    { id: 'bag', label: 'Compras' },
    { id: 'gift', label: 'Presentes' }
  ];

  TransactionType = TransactionType;

  constructor(private api: FinisApiService) {}

  ngOnInit(): void {
    this.loadCategories();
    this.refreshSub = this.api.refresh$.subscribe(() => {
      this.loadCategories();
    });
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  loadCategories(): void {
    this.loading = true;
    this.api.getCategories().subscribe({
      next: data => {
        this.categories = data;
        this.applyFilter();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    if (this.selectedTypeFilter === 0) {
      this.filteredCategories = this.categories;
    } else {
      this.filteredCategories = this.categories.filter(c => c.type === this.selectedTypeFilter);
    }
  }

  setFilter(type: number): void {
    this.selectedTypeFilter = type;
    this.applyFilter();
  }

  openCreateModal(): void {
    this.modalTitle = 'Nova Categoria';
    this.editingCategoryId = null;
    this.formName = '';
    this.formType = this.selectedTypeFilter !== 0 ? this.selectedTypeFilter : TransactionType.Diario;
    this.formColor = '#10B981';
    this.formIcon = 'tag';
    this.modalError = '';
    this.isModalOpen = true;
  }

  openEditModal(category: Category): void {
    this.modalTitle = 'Editar Categoria';
    this.editingCategoryId = category.id;
    this.formName = category.name;
    this.formType = category.type;
    this.formColor = category.colorHex;
    this.formIcon = category.icon || 'tag';
    this.modalError = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.editingCategoryId = null;
  }

  onSaveCategory(): void {
    if (!this.formName.trim()) {
      this.modalError = 'Por favor, informe o nome da categoria.';
      return;
    }

    this.modalLoading = true;
    this.modalError = '';

    const payload: Partial<Category> = {
      name: this.formName.trim(),
      type: this.formType,
      colorHex: this.formColor,
      icon: this.formIcon
    };

    if (this.editingCategoryId) {
      this.api.updateCategory(this.editingCategoryId, payload).subscribe({
        next: () => {
          this.modalLoading = false;
          this.closeModal();
          this.loadCategories();
        },
        error: err => {
          this.modalLoading = false;
          this.modalError = err.error?.message || 'Erro ao atualizar categoria.';
        }
      });
    } else {
      this.api.createCategory(payload).subscribe({
        next: () => {
          this.modalLoading = false;
          this.closeModal();
          this.loadCategories();
        },
        error: err => {
          this.modalLoading = false;
          this.modalError = err.error?.message || 'Erro ao criar categoria.';
        }
      });
    }
  }

  onDeleteCategory(cat: Category): void {
    if (confirm(`Tem certeza de que deseja excluir a categoria "${cat.name}"? Os lançamentos associados a ela passarão a ser exibidos como "Sem categoria".`)) {
      this.api.deleteCategory(cat.id).subscribe({
        next: () => {
          this.loadCategories();
        },
        error: err => {
          alert(err.error?.message || 'Erro ao excluir categoria.');
        }
      });
    }
  }
}
