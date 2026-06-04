# EVALS — NovaTech Knowledge Bot

These are the baseline evaluation questions used to verify the bot retrieves and answers correctly from the docs/ folder.

---

## Q1
**Question:** How do I reset my password?
**Source file:** docs/faq.txt
**Expected answer:** The user should go to the login page, click "Forgot Password", enter their company email, and receive a reset link within 5 minutes. If not received, check spam or contact support@novatech.io.
**Pass condition:** Response mentions "Forgot Password", email, and 5 minutes or support contact.

---

## Q2
**Question:** How many days of annual leave do full-time employees get?
**Source file:** docs/leave_policy.txt
**Expected answer:** 25 days of paid annual leave per calendar year. Up to 5 unused days can be carried over, expiring March 31.
**Pass condition:** Response mentions 25 days and carry-over rule.

---

## Q3
**Question:** What is the price of the Growth plan?
**Source file:** docs/product_overview.txt
**Expected answer:** 12 EUR per user per month, with unlimited projects and priority support.
**Pass condition:** Response mentions 12 EUR and "per user per month".

---

## Q4
**Question:** How long does it take to get a support response on the Enterprise plan?
**Source file:** docs/support_guide.txt
**Expected answer:** Enterprise customers receive a response within 1 hour, 24/7.
**Pass condition:** Response mentions 1 hour and 24/7.

---

## Q5
**Question:** What laptop does NovaTech provide to new employees?
**Source file:** docs/onboarding.txt
**Expected answer:** A MacBook Pro (M-series) or equivalent. Peripherals can be requested through the IT portal within the first 30 days.
**Pass condition:** Response mentions MacBook Pro and 30-day window for peripherals.

---

## Bonus Q6 (cross-file)
**Question:** How do I contact support outside of business hours?
**Source file:** docs/faq.txt + docs/support_guide.txt
**Expected answer:** For critical production issues, email support@novatech.io with "URGENT" in the subject. Enterprise plan has 24/7 1-hour response SLA.
**Pass condition:** Response mentions the URGENT email approach.