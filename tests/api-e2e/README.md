# Voltflow Flow-Based API-E2E Test Kütüphanesi (96 Dikey Senaryo)

Bu klasör; Voltflow projesinin uçtan uca iş mantığını, durum makinelerini (FSM), atomik transaction bütünlüğünü ve API güvenlik bariyerlerini doğrulamak üzere hazırlanmış dikey (deep-flow) hiyerarşik PowerShell test otomasyon kütüphanesini barındırır.

Mimarisi [Voltflow Kapsamlı Dry-Test Rehberi](../../docs/architecture/voltflow-dry-tests-guide.md) ve [Voltflow Workflows and Failure Modes](../../docs/architecture/voltflow-workflows-and-failure-modes.md) belgeleriyle **Voltflow Unified Taxonomy (VUT: `MM.F.SS`)** standardında 1-e-1 senkronize edilmiştir.

---

## 📁 VUT (Voltflow Unified Taxonomy) 96 Test Hiyerarşisi

Dosya isimlendirmeleri `[MM]_[F][SS]_<senaryo_tanımı>.ps1` standardındadır:

```text
tests/api-e2e/
├── 01-IdentityAndAuthFlows/
│   ├── 01-LoginAndApproval/
│   │   ├── 01_101_wrong_password_returns_401.ps1
│   │   ├── 01_102_unapproved_user_returns_403.ps1
│   │   ├── 01_103_login_missing_email_returns_400.ps1
│   │   ├── 01_104_registration_missing_password_returns_400.ps1
│   │   ├── 01_201_master_otp_registration_200.ps1
│   │   ├── 01_202_invalid_otp_creates_unapproved_user.ps1
│   │   ├── 01_203_duplicate_email_registration_409.ps1
│   │   ├── 01_204_registered_approved_user_login_200.ps1
│   │   ├── 01_403_admin_approves_unapproved_user.ps1
│   │   ├── 01_404_non_admin_approval_rejected_403.ps1
│   │   ├── 01_405_assign_role_by_admin.ps1
│   │   ├── 01_406_invalid_role_assignment_rejected_422.ps1
│   │   └── 01_407_approve_nonexistent_user_returns_404.ps1
│   └── 02-SessionManagement/
│       ├── 01_301_session_revocation_204.ps1
│       ├── 01_302_revoked_token_access_returns_401.ps1
│       ├── 01_303_unauthenticated_access_returns_401.ps1
│       ├── 01_304_invalid_jwt_token_returns_401.ps1
│       ├── 01_401_password_reset_request_and_complete_200.ps1
│       └── 01_402_used_or_invalid_reset_token_rejected_400.ps1
├── 02-QuoteToOrderFlows/
│   ├── 01-QuoteValidation/
│   │   ├── 02_101_customer_lead_to_active_conversion.ps1
│   │   ├── 02_102_duplicate_customer_email_rejected_422.ps1
│   │   ├── 02_103_customer_get_by_id_and_404.ps1
│   │   ├── 02_104_customer_list_returns_200.ps1
│   │   ├── 02_105_create_customer_missing_email_rejected_422.ps1
│   │   ├── 02_201_quote_list_returns_200.ps1
│   │   ├── 02_202_quote_get_by_id_returns_404.ps1
│   │   ├── 02_203_create_quote_draft_and_add_items.ps1
│   │   ├── 02_301_empty_quote_issue_rejected_400.ps1
│   │   └── 02_302_quote_item_invalid_price_or_quantity_rejected_422.ps1
│   └── 02-AcceptanceAndDeposit/
│       ├── 02_401_quote_items_total_and_accept_with_deposit.ps1
│       ├── 02_402_quote_rejection_with_reason.ps1
│       ├── 02_403_quote_expire_transition.ps1
│       ├── 02_404_invalid_deposit_payment_rejected_422.ps1
│       ├── 02_405_accept_already_accepted_quote_rejected_422.ps1
│       ├── 02_406_accept_rejected_quote_returns_422.ps1
│       ├── 02_407_accept_expired_quote_returns_422.ps1
│       ├── 02_501_quote_to_work_order_idempotent_conversion.ps1
│       ├── 02_502_unaccepted_quote_to_work_order_rejected_422.ps1
│       ├── 02_601_project_creation_and_phase_management.ps1
│       ├── 02_602_project_list_returns_200.ps1
│       └── 02_603_project_get_by_id_returns_404.ps1
├── 03-WorkOrderExecutionFlows/
│   ├── 01-StateTransitions/
│   │   ├── 03_101_safety_checklist_gate_enforcement.ps1
│   │   ├── 03_102_work_order_assignment_and_enroute.ps1
│   │   ├── 03_103_noshow_reporting_and_state_transition.ps1
│   │   ├── 03_104_enroute_without_assigned_rejected_422.ps1
│   │   ├── 03_105_work_order_list_returns_200.ps1
│   │   ├── 03_106_work_order_get_by_id_returns_404.ps1
│   │   ├── 03_107_create_work_order_success.ps1
│   │   └── 03_108_full_state_machine_happy_path.ps1
│   ├── 02-CheckInAndSafety/
│   │   ├── 03_201_time_tracking_and_double_checkin_prevention.ps1
│   │   ├── 03_202_checkout_without_checkin_rejected_422.ps1
│   │   ├── 03_301_hold_state_auto_checkout_compensation.ps1
│   │   ├── 03_302_field_material_addition_to_work_order.ps1
│   │   └── 03_303_add_material_to_completed_work_order_rejected_422.ps1
│   └── 03-ReviewAndClosing/
│       ├── 03_401_work_order_completion_with_proof_and_outbox.ps1
│       ├── 03_402_complete_without_proof_rejected_422.ps1
│       ├── 03_501_review_gate_and_terminal_invoiced_state.ps1
│       ├── 03_502_work_order_cancellation_and_terminal_guard.ps1
│       ├── 03_503_resume_unheld_work_order_rejected_422.ps1
│       ├── 03_504_invoice_unapproved_work_order_rejected_422.ps1
│       ├── 03_505_start_cancelled_work_order_rejected_422.ps1
│       └── 03_506_approve_billing_after_completion.ps1
├── 04-InventoryFlows/
│   ├── 01-StockBoundaries/
│   │   ├── 04_101_negative_stock_gate_and_atomic_rollback.ps1
│   │   ├── 04_102_get_stock_by_material_code_and_404.ps1
│   │   ├── 04_103_stock_adjust_positive_delta.ps1
│   │   └── 04_104_stock_adjust_zero_delta_rejected_422.ps1
│   └── 02-Reservations/
│       ├── 04_201_stock_reservation_and_available_reduction.ps1
│       ├── 04_202_insufficient_stock_reservation_rejected.ps1
│       ├── 04_203_stock_reservation_zero_quantity_rejected_422.ps1
│       ├── 04_204_stock_reservation_negative_quantity_rejected_422.ps1
│       └── 04_205_sequential_reservations_reduce_available.ps1
├── 05-FinanceAndLedgerFlows/
│   ├── 01-PaymentAllocations/
│   │   ├── 05_101_list_invoices_by_customer.ps1
│   │   ├── 05_102_billing_entry_creation_for_project.ps1
│   │   ├── 05_103_list_payments_by_customer.ps1
│   │   ├── 05_104_list_billing_entries_by_project.ps1
│   │   ├── 05_201_customer_payment_and_ledger_credit.ps1
│   │   ├── 05_202_zero_or_negative_payment_amount_rejected_422.ps1
│   │   ├── 05_203_payment_with_invalid_customer_rejected_422.ps1
│   │   ├── 05_204_invalid_payment_method_rejected_422.ps1
│   │   ├── 05_301_payment_allocation_to_invoice.ps1
│   │   ├── 05_302_over_allocation_rejected.ps1
│   │   ├── 05_303_allocation_exceeding_invoice_remaining_rejected_422.ps1
│   │   ├── 05_304_allocation_to_nonexistent_invoice_rejected.ps1
│   │   └── 05_305_zero_amount_allocation_rejected_422.ps1
│   └── 02-Concurrency/
│       └── 05_401_optimistic_concurrency_duplicate_allocation_rejected.ps1
├── 07-OperationsAndOutboxFlows/
│   └── 01-DeadLetterAndReplay/
│       ├── 07_201_operations_dead_letter_outbox_query.ps1
│       ├── 07_202_non_admin_operations_dead_letter_rejected_403.ps1
│       └── 07_203_outbox_replay_nonexistent_returns_404.ps1
├── 08-SecurityAndResilienceFlows/
│   ├── 01-ExecutionGuard/
│   │   ├── 08_101_idempotency_key_payload_mismatch_rejected_409.ps1
│   │   └── 08_201_mutation_rate_limit_exceeded_returns_429.ps1
│   └── 02-ErrorStandards/
│       ├── 08_401_rfc7807_problem_details_structure_compliance.ps1
│       ├── 08_402_invalid_content_type_returns_415.ps1
│       ├── 08_403_empty_body_post_returns_400.ps1
│       ├── 08_404_malformed_json_returns_400.ps1
│       ├── 08_405_nonexistent_endpoint_returns_404.ps1
│       └── 08_406_method_not_allowed_returns_405.ps1
```

---

## 🚀 Testleri Çalıştırma

### 1. Tüm 96 Testi Tek Komutla Koşmak:
```powershell
pwsh -File .\tests\api-e2e\run-all.ps1
```

### 2. Modül Bazlı Çalıştırmak:
```powershell
# Sadece Modül 1 (Identity & Auth) testlerini koşmak:
Get-ChildItem -Path .\tests\api-e2e\01-IdentityAndAuthFlows -Recurse -Filter '*.ps1' | ForEach-Object { pwsh -File $_.FullName }

# Sadece Modül 4 (Inventory) testlerini koşmak:
Get-ChildItem -Path .\tests\api-e2e\04-InventoryFlows -Recurse -Filter '*.ps1' | ForEach-Object { pwsh -File $_.FullName }
```

---

## 📖 12-Halka Mimari Senkronizasyon Referansları
* 📘 [Voltflow Kapsamlı Dry-Test Rehberi](../../docs/architecture/voltflow-dry-tests-guide.md)
* 📗 [Voltflow İş Akışları ve Hata Modları](../../docs/architecture/voltflow-workflows-and-failure-modes.md)
* 📙 [Master VUT Taxonomy](../../src/Voltflow.Domain/Common/VoltflowTaxonomy.cs)
* 📕 [UI-E2E Test Paketi](../ui-e2e/README.md)
* 📒 [Backend C# Test Katmanı](../backend/README.md)
