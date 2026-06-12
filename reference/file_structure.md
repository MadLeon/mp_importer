# 文件格式

Manufacture Process 文件大致分为上下两部分
- 信息区
- 生产过程区

## 信息区

信息区包含订单和图纸信息:
- B7 → purchase_order.po_number
- N6 → purchase_order.oe_number
- Q6 → job.job_number
- F7 → order_item.line_number
- J7 → part.drawing_number
- R7 → part.revision
- B8 → order_item.drawing_release_date
- H8 → part.description
- D9 → order_item.delivery_required_date
- Q9 → order_item.quantity 与每一级 part_tree.quantity 的乘积

## 生产过程区

- 每个生产过程占一行, 行位于第11行到第26行
- D 列为 shop code, E 列为 row number
- shop code 代表步骤的类别, 具体指代如下:
    - FI = Free Issued Material
    - P = Purchase
    - RT = Record to Manufacture
    - SC = Subcontract
    - I = Inspect
    - H = Hold
    - W = Witness
    - PI = Packaging Inspection
- E 列为以10为单位的递增值, 类似于行号
- F 到 N 为合并左对齐的步骤内容