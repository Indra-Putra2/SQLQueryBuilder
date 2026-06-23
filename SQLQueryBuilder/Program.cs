// Fixed the string concatenation to have proper spaces and correct closing parenthesis
var test = "c.customer_id," +
    "CONCAT(c.first_name, ' ', c.last_name) AS customer_full_name," +
    "c.email_address AS contact_email, " +
    "COUNT(o.order_id) AS total_orders_placed," +
    "COUNT(o.customer_id)," +
    "SUM(o.order_amount) AS \"Gross Revenue ($)\"," +
    "CASE " +
    "WHEN SUM(o.order_amount) >= 1000 THEN SUM(o.order_amount) * 0.85 " +
    "ELSE SUM(o.order_amount) " +
    "END AS net_revenue_after_discounts";

var wtf = "usr.user_id," +
    "    COALESCE(usr.display_name, UPPER(CONCAT(usr.first_name, ' ', NULLIF(usr.last_name, '')))) AS formatted_name," +
    "    stats.total_revenue," +
    "    (" +
    "        SELECT COUNT(DISTINCT log_id) " +
    "        FROM user_logs " +
    "        WHERE user_id = usr.user_id AND action_date >= DATE_SUB(NOW(), INTERVAL 30 DAY)" +
    "    ) AS recent_activity_count," +
    "    ROUND(" +
    "        (stats.total_revenue - stats.total_cost) / NULLIF(" +
    "            (SELECT SUM(target_amount) FROM budgets WHERE budget_year = 2026 AND (department_id = usr.dept_id OR is_global = 1)), " +
    "            0" +
    "        ) * 100, " +
    "        2" +
    "    ) AS margin_vs_budget_percentage," +
    "    CASE " +
    "        WHEN stats.total_revenue > 1000000 AND (usr.role = 'Admin' OR usr.is_vip = 1) THEN 'Tier 1'" +
    "        WHEN stats.total_revenue BETWEEN 50000 AND 1000000 THEN CONCAT('Tier 2 - ', LOWER(usr.region_code))" +
    "        ELSE 'Tier 3'" +
    "    END AS customer_segmentation," +
    "    DENSE_RANK() OVER (" +
    "        PARTITION BY usr.dept_id, CASE WHEN usr.country IN ('US', 'CA') THEN 'NA' ELSE 'INTL' END " +
    "        ORDER BY (stats.total_revenue - stats.total_cost) DESC, usr.created_at ASC" +
    "    ) AS internal_dept_rank";

var test2 = "(SELECT COUNT(*) FROM orders o WHERE o.customer_id = c.id) AS total_orders";

